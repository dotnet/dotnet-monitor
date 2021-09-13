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
using Utils = Microsoft.Diagnostics.Monitoring.WebApi.Utilities;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions
{
    internal sealed class CollectGCDumpAction : ICollectionRuleAction<CollectGCDumpOptions>
    {
        private readonly IDiagnosticServices _diagnosticServices;
        private readonly IServiceProvider _serviceProvider;

        public CollectGCDumpAction(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _diagnosticServices = serviceProvider.GetRequiredService<IDiagnosticServices>();
        }

        public async Task<CollectionRuleActionResult> ExecuteAsync(CollectGCDumpOptions options, IEndpointInfo endpointInfo, CancellationToken token)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (endpointInfo == null)
            {
                throw new ArgumentNullException(nameof(endpointInfo));
            }

            string egress = options.Egress;

            ValidationContext context = new(options, _serviceProvider, items: null);
            Validator.ValidateObject(options, context, validateAllProperties: true);

            string gcdumpFileName = Utils.GenerateGCDumpFileName; //FormattableString.Invariant($"{GetFileNameTimeStampUtcNow()}_{endpointInfo.ProcessId}.gcdump");

            string gcdumpFilePath = string.Empty;

            Func<CancellationToken, Task<IFastSerializable>> action = async (token) => await DiagController.GetGCHeadDump(endpointInfo, token);

            KeyValueLogScope scope = Utils.GetScope(Utils.ArtifactType_Dump, endpointInfo);

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

        // Move this to be a shared method, maybe in Utilities?
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
