// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Controllers;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions
{
    internal sealed class CollectDumpAction : ICollectionRuleAction<CollectDumpOptions>
    {
        private readonly IDiagnosticServices _diagnosticServices;
        private readonly IServiceProvider _serviceProvider;

        public CollectDumpAction(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _diagnosticServices = serviceProvider.GetRequiredService<IDiagnosticServices>();
        }

        public async Task<CollectionRuleActionResult> ExecuteAsync(CollectDumpOptions options, IEndpointInfo endpointInfo, CancellationToken token)
        {
            DumpType dumpType = options.Type.GetValueOrDefault(CollectDumpOptionsDefaults.Type);
            string egressProvider = options.Egress;

            IProcessInfo processInfo = await _diagnosticServices.GetProcessAsync(new ProcessKey(endpointInfo.ProcessId), token);

            string dumpFileName = DiagController.GenerateDumpFileName();

            string dumpFilePath = "";

            // Given our options validation, I believe this is probably redundant...should I remove it?
            if (string.IsNullOrEmpty(egressProvider))
            {
                throw new ArgumentException("No Egress Provider was supplied.");
            }
            else
            {
                KeyValueLogScope scope = DiagController.GetDumpScope(processInfo);

                dumpFilePath = await SendToEgress(new EgressOperation(
                    token => _diagnosticServices.GetDump(processInfo, dumpType, token),
                    egressProvider,
                    dumpFileName,
                    processInfo.EndpointInfo,
                    ContentTypes.ApplicationOctetStream,
                    scope), token);
            }

            return new CollectionRuleActionResult()
            {
                OutputValues = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    { "EgressPath", dumpFilePath }
                }
            };
        }

        private async Task<string> SendToEgress(EgressOperation egressStreamResult, CancellationToken token)
        {
            var result = await egressStreamResult.ExecuteAsync(_serviceProvider, token);

            return result.Result.Value;
        }
    }
}
