// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.NETCore.Client;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress
{
    /// <summary>
    /// Egress service implementation required by the REST server.
    /// </summary>
    internal class EgressService : IEgressService
    {
        private readonly EgressProviderSource _source;

        public EgressService(EgressProviderSource source)
        {
            _source = source;

            _source.Initialize();
        }

        public void ValidateProviderExists(string providerName)
        {
            // GetProviderType should never return null so no need to check; it will throw
            // if the egress provider could not be located or instantiated.
            _ = _source.GetEgressProvider(providerName);
        }

        public async Task<EgressResult> EgressAsync(string providerName, Func<Stream, CancellationToken, Task> action, string fileName, string contentType, IEndpointInfo source, CollectionRuleMetadata collectionRuleMetadata, CancellationToken token)
        {
            IEgressExtension extension = _source.GetEgressProvider(providerName);

            EgressArtifactResult result = await extension.EgressArtifact(
                providerName,
                await CreateSettings(source, fileName, contentType, collectionRuleMetadata, token),
                action,
                token);

            if (!result.Succeeded)
            {
                throw new EgressException(Strings.ErrorMessage_EgressExtensionFailed);
            }

            return new EgressResult(result.ArtifactPath);
        }

        public async Task ValidateProviderOptionsAsync(string providerName, CancellationToken token)
        {
            IEgressExtension extension = _source.GetEgressProvider(providerName);

            EgressArtifactResult result = await extension.ValidateProviderAsync(
                providerName,
                new EgressArtifactSettings(),
                token);

            if (!result.Succeeded)
            {
                throw new EgressException(string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_EgressProviderFailedValidation, providerName, result.FailureMessage));
            }
        }

        private static async Task<EgressArtifactSettings> CreateSettings(IEndpointInfo source, string fileName, string contentType, CollectionRuleMetadata collectionRuleMetadata, CancellationToken token)
        {
            EgressArtifactSettings settings = new();
            settings.Name = fileName;
            settings.ContentType = contentType;

            if (source.TargetFrameworkSupportsProcessEnv())
            {
                DiagnosticsClient client = new DiagnosticsClient(source.Endpoint);
                settings.EnvBlock = await client.GetProcessEnvironmentAsync(token);
            }

            // Activity metadata
            Activity activity = Activity.Current;
            if (null != activity)
            {
                AddMetadata(settings, ActivityMetadataNames.ParentId, activity.GetParentId());
                AddMetadata(settings, ActivityMetadataNames.SpanId, activity.GetSpanId());
                AddMetadata(settings, ActivityMetadataNames.TraceId, activity.GetTraceId());
            }

            if (null != collectionRuleMetadata)
            {
                AddMetadata(settings, CollectionRuleMetadataNames.CollectionRuleName, collectionRuleMetadata.CollectionRuleName);
                AddMetadata(settings, CollectionRuleMetadataNames.ActionListIndex, collectionRuleMetadata.ActionListIndex.ToString("D", CultureInfo.InvariantCulture));
                AddMetadata(settings, CollectionRuleMetadataNames.ActionName, collectionRuleMetadata.ActionName);
            }

            // Artifact metadata
            AddMetadata(settings, ArtifactMetadataNames.ArtifactSource.ProcessId, source.ProcessId.ToString(CultureInfo.InvariantCulture));
            AddMetadata(settings, ArtifactMetadataNames.ArtifactSource.RuntimeInstanceCookie, source.RuntimeInstanceCookie.ToString("N"));

            return settings;
        }

        private static void AddMetadata(EgressArtifactSettings settings, string key, string value)
        {
            settings.Metadata.Add($"{ToolIdentifiers.StandardPrefix}{key}", value);
        }
    }
}
