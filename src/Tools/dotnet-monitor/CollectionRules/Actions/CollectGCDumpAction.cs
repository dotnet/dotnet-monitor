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
    internal sealed class CollectGCDumpAction : ICollectionRuleAction<CollectGCDumpOptions>
    {
        private readonly IDiagnosticServices _diagnosticServices;
        private readonly IServiceProvider _serviceProvider;

        public const string ArtifactType_GCDump = "gcdump";

        public CollectGCDumpAction(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _diagnosticServices = serviceProvider.GetRequiredService<IDiagnosticServices>();
        }

        public async Task<CollectionRuleActionResult> ExecuteAsync(CollectGCDumpOptions options, IEndpointInfo endpointInfo, CancellationToken token)
        {
            string egress = options.Egress; // Do we need to check for non-null value? -> be consistent with what we do for Dump.

            if (string.IsNullOrEmpty(egress))
            {
                throw new ArgumentException("No Egress Provider was supplied.");
            }

            int pid = endpointInfo.ProcessId;

            ProcessKey? processKey = new ProcessKey(pid);

            IProcessInfo processInfo = await _diagnosticServices.GetProcessAsync(processKey, token);

            string gcdumpFileName = FormattableString.Invariant($"{GetFileNameTimeStampUtcNow()}_{processInfo.EndpointInfo.ProcessId}.gcdump");

            string gcdumpFilePath = string.Empty;

            Func<CancellationToken, Task<IFastSerializable>> action = async (token) => await DiagController.GetGCHeadDump(endpointInfo, token);

            KeyValueLogScope scope = new KeyValueLogScope();
            scope.AddArtifactType(ArtifactType_GCDump);
            scope.AddEndpointInfo(endpointInfo);

            EgressOperation egressOperation = new EgressOperation(
                        ConvertFastSerializeAction(action),
                        egress,
                        gcdumpFileName,
                        endpointInfo,
                        ContentTypes.ApplicationOctetStream,
                        scope);

            ExecutionResult<EgressResult> result = await egressOperation.ExecuteAsync(_serviceProvider, token);

            gcdumpFilePath = result.Result.Value;

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
