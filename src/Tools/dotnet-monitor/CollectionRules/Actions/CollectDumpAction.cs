// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Controllers;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions
{
    internal sealed class CollectDumpAction : ControllerBase,
        ICollectionRuleAction<CollectDumpOptions>
    {
        public const string ArtifactType_Dump = "dump";

        private readonly ILogger<CollectDumpAction> _logger;
        private readonly IDiagnosticServices _diagnosticServices;
        private readonly EgressOperationStore _operationsStore;

        public CollectDumpAction(ILogger<CollectDumpAction> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _diagnosticServices = serviceProvider.GetRequiredService<IDiagnosticServices>();
            //_diagnosticPortOptions = serviceProvider.GetService<IOptions<DiagnosticPortOptions>>();
            _operationsStore = serviceProvider.GetRequiredService<EgressOperationStore>();
        }

        public async Task<CollectionRuleActionResult> ExecuteAsync(CollectDumpOptions options, IEndpointInfo endpointInfo, CancellationToken token)
        {
            DumpType dumpType = options.Type.GetValueOrDefault(CollectDumpOptionsDefaults.Type);
            string egressProvider = options.Egress; // I believe we should be doing a check for a non-null value (or maybe a valid one)?

            int pid = endpointInfo.ProcessId;

            ProcessKey? processKey = new ProcessKey(pid);

            IProcessInfo processInfo = await _diagnosticServices.GetProcessAsync(processKey, token);

            string dumpFileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                FormattableString.Invariant($"dump_{GetFileNameTimeStampUtcNow()}.dmp") :
                FormattableString.Invariant($"core_{GetFileNameTimeStampUtcNow()}");

            string dumpFilePath = "";

            if (string.IsNullOrEmpty(egressProvider))
            {
                throw new ArgumentException("No Egress Provider was supplied.");
            }
            else
            {
                KeyValueLogScope scope = new KeyValueLogScope();
                scope.AddArtifactType(ArtifactType_Dump);
                scope.AddEndpointInfo(processInfo.EndpointInfo);

                dumpFilePath = await SendToEgress(new EgressOperation(
                    token => _diagnosticServices.GetDump(processInfo, dumpType, token),
                    egressProvider,
                    dumpFileName,
                    processInfo.EndpointInfo,
                    ContentTypes.ApplicationOctetStream,
                    scope), limitKey: ArtifactType_Dump);
            }

            return new CollectionRuleActionResult()
            {
                OutputValues = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    { "EgressPath", dumpFilePath }
                }
            };
        }

        private static string GetFileNameTimeStampUtcNow()
        {
            return DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        }

        private async Task<string> SendToEgress(EgressOperation egressStreamResult, string limitKey)
        {
            // Will throw TooManyRequestsException if there are too many concurrent operations.
            Guid operationId = await _operationsStore.AddOperation(egressStreamResult, limitKey);
            string newUrl = this.Url.Action(
                action: nameof(OperationsController.GetOperationStatus),
                controller: OperationsController.ControllerName, new { operationId = operationId },
                protocol: this.HttpContext.Request.Scheme, this.HttpContext.Request.Host.ToString());

            return newUrl; // Switched to returning the URL so that we can include it in the CollectionRuleActionResult
            //return Accepted(newUrl);
        }
    }
}
