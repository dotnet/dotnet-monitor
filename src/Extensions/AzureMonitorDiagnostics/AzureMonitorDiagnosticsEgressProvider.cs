// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure;
using Azure.Monitor.Diagnostics;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Diagnostics.Monitoring.Extension.Common;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Runtime.InteropServices;
using static System.Globalization.CultureInfo;

namespace Microsoft.Diagnostics.Monitoring.AzureMonitorDiagnostics;

/// <summary>
/// Egress provider for uploading artifacts to Azure Monitor's Diagnostic services.
/// </summary>
/// <remarks>
/// Azure Monitor Diagnostic Services covers Application Insights Profiler and
/// Application Insights Snapshot Debugger.
/// </remarks>
internal sealed class AzureMonitorDiagnosticsEgressProvider : EgressProvider<AzureMonitorDiagnosticsEgressProviderOptions>
{
    /// <summary>
    /// Construct a new <see cref="AzureMonitorDiagnosticsEgressProvider"/> instance.
    /// </summary>
    /// <param name="logger">A logger.</param>
    public AzureMonitorDiagnosticsEgressProvider(ILogger logger) : base(logger)
    {
    }

    /// <summary>
    /// The main method for an egress provider. Uploads a stream to the Azure
    /// Monitor Diagnostic Services endpoint.
    /// </summary>
    /// <param name="options">Options for this provider.</param>
    /// <param name="action">A delegate that writes the artifact to a stream.</param>
    /// <param name="artifactSettings">Properties for the egressed artifact.</param>
    /// <param name="token">A cancellation token.</param>
    /// <returns></returns>
    public override async Task<string> EgressAsync(
        AzureMonitorDiagnosticsEgressProviderOptions options,
        Func<Stream, CancellationToken, Task> action,
        EgressArtifactSettings artifactSettings,
        CancellationToken token)
    {
        ConnectionString connectionString = options.ValidatedConnectionString!;
        string iKey = connectionString.InstrumentationKey;

        // TODO: Create the appropriate TokenCredential from auth options.
        DiagnosticsClientOptions clientOptions = new()
        {
            Endpoint = ResolveDiagnosticsEndpoint(connectionString)
        };

        DiagnosticsClient client = new(clientOptions);

        // Use the name as the artifact ID, if it's a Guid.
        if (!Guid.TryParse(artifactSettings.Name, out Guid artifactId))
        {
            // Otherwise create our own.
            artifactId = Guid.NewGuid();
        }

        UploadToken uploadToken = await client.GetUploadTokenAsync(iKey, ArtifactKind.Profile, artifactId, token);
        BlockBlobClient blobClient = new(uploadToken.BlobUri);

        BlobHttpHeaders httpHeaders = new()
        {
            ContentType = artifactSettings.ContentType
        };

        switch (options.Compression)
        {
            case CompressionType.GZip:
                httpHeaders.ContentEncoding = "gzip";
                action = WrapActionWithGZipCompression(action);
                break;

            default:
                break;
        }

        BlockBlobOpenWriteOptions blobOptions = new()
        {
            HttpHeaders = httpHeaders
        };

        using (Stream blobStream = await blobClient.OpenWriteAsync(overwrite: true, blobOptions, token))
        {
            _logger.EgressProviderInvokeStreamAction(Constants.Provider.Name);
            await action(blobStream, token);
            await blobStream.FlushAsync(token);
        }


        BlobInfo blobInfo;
        try
        {
            Dictionary<string, string> metadata = MergeMetadata(artifactSettings);
            blobInfo = await blobClient.SetMetadataAsync(metadata, cancellationToken: token);
        }
        catch (Exception ex) when (ex is InvalidOperationException or RequestFailedException)
        {
            _logger.InvalidMetadata(ex);
            blobInfo = await blobClient.SetMetadataAsync(artifactSettings.Metadata, cancellationToken: token);
        }

        ArtifactAccepted artifactInfo = await client.CommitUploadAsync(iKey, ArtifactKind.Profile, artifactId, blobInfo.ETag, token);
        _logger.EgressProviderSavedStream(Constants.Provider.Name, artifactInfo.ArtifactLocationId!);
        return artifactInfo.ArtifactLocationId!;
    }

    /// <summary>
    /// Wrap the given action in an action that compresses the stream.
    /// </summary>
    /// <param name="action">The original action over the stream.</param>
    /// <returns>The wrapped action.</returns>
    private static Func<Stream, CancellationToken, Task> WrapActionWithGZipCompression(Func<Stream, CancellationToken, Task> action)
    {
        return async (Stream stream, CancellationToken token) =>
        {
            using GZipStream zipStream = new(stream, CompressionLevel.Fastest, leaveOpen: true);
            await action(zipStream, token);
        };
    }

    /// <summary>
    /// Create a metadata dictionary from the artifact settings.
    /// </summary>
    /// <param name="artifactSettings">The artifact settings.</param>
    /// <returns>A dictionary containing metadata for the uploaded artifact.</returns>
    private Dictionary<string, string> MergeMetadata(EgressArtifactSettings artifactSettings)
    {
        Dictionary<string, string> mergedMetadata = new(artifactSettings.Metadata);
        foreach (KeyValuePair<string, string> metadataPair in artifactSettings.CustomMetadata)
        {
            if (!mergedMetadata.ContainsKey(metadataPair.Key))
            {
                mergedMetadata[metadataPair.Key] = metadataPair.Value;
            }
            else
            {
                _logger.DuplicateKeyInMetadata(metadataPair.Key);
            }
        }

        // Required metadata for artifact handling.
        mergedMetadata[Constants.Metadata.TraceFileFormat] = Constants.TraceFormat.NetTrace;
        mergedMetadata[Constants.Metadata.MachineName] = GetMachineName(artifactSettings);
        mergedMetadata[Constants.Metadata.OSPlatform] = GetOSPlatform();

        // TODO: This should be the processor architecture of the target application.
        mergedMetadata[Constants.Metadata.ProcessorArch] = RuntimeInformation.ProcessArchitecture.ToString();

        // TODO: This should be the triggering time for the trace.
        mergedMetadata[Constants.Metadata.TraceStartTime] = DateTime.UtcNow.ToString("O", InvariantCulture);

        // TODO: Determine the trigger type
        mergedMetadata[Constants.Metadata.TriggerType] = "Default";

        return mergedMetadata;
    }

    /// <summary>
    /// Gets the machine name to use in the metadata of the uploaded artifact.
    /// </summary>
    /// <param name="artifactSettings">The artifact settings.</param>
    /// <returns>The machine name.</returns>
    private static string GetMachineName(EgressArtifactSettings artifactSettings)
    {
        // Try to get the machine name from the environment block of the application (not this process)
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && artifactSettings.EnvBlock.TryGetValue("COMPUTERNAME", out string? computerName))
        {
            return computerName;
        }

        // Fall back to getting the machine name from this process.
        return Environment.MachineName;
    }

    private static string GetOSPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return nameof(OSPlatform.Linux);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return nameof(OSPlatform.Windows);
        }

        return "Unknown";
    }

    /// <summary>
    /// Resolve the endpoint for uploading artifacts from a connection string.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <returns>The endpoint.</returns>
    private static Uri ResolveDiagnosticsEndpoint(ConnectionString? connectionString)
    {
        // May be overridden by an environment variable
        string? endpointOverride = Environment.GetEnvironmentVariable("ApplicationInsightsProfilerEndpoint");
        if (!string.IsNullOrEmpty(endpointOverride) && Uri.TryCreate(endpointOverride, UriKind.Absolute, out Uri? endpoint))
        {
            return endpoint;
        }

        endpoint = connectionString?.GetFeatureEndpoint("Profiler");
        return endpoint ?? new Uri("https://profiler.monitor.azure.com/");
    }
}
