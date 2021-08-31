// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using FastSerialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Controllers;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.WebApi.Validation;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions
{
    internal sealed class CollectGCDumpAction : ControllerBase,
        ICollectionRuleAction<CollectGCDumpOptions>
    {
        private readonly ILogger<CollectGCDumpAction> _logger;
        private readonly IDiagnosticServices _diagnosticServices;
        private readonly EgressOperationStore _operationsStore;

        public const string ArtifactType_GCDump = "gcdump";

        public CollectGCDumpAction(ILogger<CollectGCDumpAction> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _diagnosticServices = serviceProvider.GetRequiredService<IDiagnosticServices>();
            //_diagnosticPortOptions = serviceProvider.GetService<IOptions<DiagnosticPortOptions>>();
            _operationsStore = serviceProvider.GetRequiredService<EgressOperationStore>();
        }
        public async Task<CollectionRuleActionResult> ExecuteAsync(CollectGCDumpOptions options, IEndpointInfo endpointInfo, CancellationToken token)
        {
            string egress = options.Egress; // Need to check for non-null value

            int pid = endpointInfo.ProcessId;

            ProcessKey? processKey = new ProcessKey(pid);

            IProcessInfo processInfo = await _diagnosticServices.GetProcessAsync(processKey, token);

            string fileName = FormattableString.Invariant($"{GetFileNameTimeStampUtcNow()}_{processInfo.EndpointInfo.ProcessId}.gcdump");

            Func<CancellationToken, Task<IFastSerializable>> action = async (token) => {
                var graph = new Graphs.MemoryGraph(50_000);

                EventGCPipelineSettings settings = new EventGCPipelineSettings
                {
                    Duration = Timeout.InfiniteTimeSpan,
                };

                var client = new DiagnosticsClient(processInfo.EndpointInfo.Endpoint);

                await using var pipeline = new EventGCDumpPipeline(client, settings, graph);
                await pipeline.RunAsync(token);

                return new GCHeapDump(graph)
                {
                    CreationTool = "dotnet-monitor"
                };
            };

            string gcdumpFilePath = await Result(
                ArtifactType_GCDump,
                egress,
                ConvertFastSerializeAction(action),
                fileName,
                ContentTypes.ApplicationOctetStream,
                processInfo.EndpointInfo);

            return new CollectionRuleActionResult()
            {
                OutputValues = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    { "EgressPath", gcdumpFilePath }
                }
            };

        }

        private static string GetFileNameTimeStampUtcNow()
        {
            return DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        }

        private async Task<string> Result(
            string artifactType,
            string providerName,
            Func<Stream, CancellationToken, Task> action,
            string fileName,
            string contentType,
            IEndpointInfo endpointInfo,
            bool asAttachment = true)
        {
            KeyValueLogScope scope = new KeyValueLogScope();
            scope.AddArtifactType(artifactType);
            scope.AddEndpointInfo(endpointInfo);

            return await SendToEgress(new EgressOperation(
                action,
                providerName,
                fileName,
                endpointInfo,
                contentType,
                scope),
                limitKey: artifactType);
        }

        private async Task<string> SendToEgress(EgressOperation egressStreamResult, string limitKey)
        {
            // Will throw TooManyRequestsException if there are too many concurrent operations.
            Guid operationId = await _operationsStore.AddOperation(egressStreamResult, limitKey);
            string newUrl = this.Url.Action(
                action: nameof(OperationsController.GetOperationStatus),
                controller: OperationsController.ControllerName, new { operationId = operationId },
                protocol: this.HttpContext.Request.Scheme, this.HttpContext.Request.Host.ToString());

            return newUrl; // switched to returning URL so we can include it in the CollectionRuleActionResult
        }

        private static Func<Stream, CancellationToken, Task> ConvertFastSerializeAction(Func<CancellationToken, Task<IFastSerializable>> action)
        {
            return async (stream, token) =>
            {
                IFastSerializable fastSerializable = await action(token);

                // FastSerialization requests the length of the stream before serializing to the stream.
                // If the stream is a response stream, requesting the length or setting the position is
                // not supported. Create an intermediate buffer if testing the stream fails.
                // This can use a huge amount of memory if the IFastSerializable is very large.
                // CONSIDER: Update FastSerialization to not get the length or attempt to reset the position.
                bool useIntermediateStream = false;
                try
                {
                    _ = stream.Length;
                }
                catch (NotSupportedException)
                {
                    useIntermediateStream = true;
                }

                if (useIntermediateStream)
                {
                    using var intermediateStream = new MemoryStream();

                    var serializer = new Serializer(intermediateStream, fastSerializable, leaveOpen: true);
                    serializer.Close();

                    intermediateStream.Position = 0;

                    await intermediateStream.CopyToAsync(stream, 0x10000, token);
                }
                else
                {
                    var serializer = new Serializer(stream, fastSerializable, leaveOpen: true);
                    serializer.Close();
                }
            };
        }
    }
}
