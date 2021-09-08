// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Exceptions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Utils = Microsoft.Diagnostics.Monitoring.WebApi.Utilities;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions
{
    internal sealed class CollectDumpAction : ICollectionRuleAction<CollectDumpOptions>
    {
        private readonly IDumpService _dumpService;
        private readonly IServiceProvider _serviceProvider;

        public CollectDumpAction(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _dumpService = serviceProvider.GetRequiredService<IDumpService>();
        }

        public async Task<CollectionRuleActionResult> ExecuteAsync(CollectDumpOptions options, IEndpointInfo endpointInfo, CancellationToken token)
        {
            DumpType dumpType = options.Type.GetValueOrDefault(CollectDumpOptionsDefaults.Type);
            string egressProvider = options.Egress;

            string dumpFileName = Utils.GenerateDumpFileName();

            string dumpFilePath = string.Empty;

            // Given our options validation, I believe this is probably redundant...should I remove it?
            if (string.IsNullOrEmpty(egressProvider))
            {
                // Also, I would move this to Strings.resx if we do keep it, but I decided to wait for feedback before doing that.
                throw new ArgumentException("No Egress Provider was supplied.");
            }
            else
            {
                KeyValueLogScope scope = Utils.GetScope(Utils.ArtifactType_Dump, endpointInfo);

                try
                {
                    EgressOperation egressOperation = new EgressOperation(
                        token => _dumpService.DumpAsync(endpointInfo, dumpType, token),
                        egressProvider,
                        dumpFileName,
                        endpointInfo,
                        ContentTypes.ApplicationOctetStream,
                        scope);

                    ExecutionResult<EgressResult> result = await egressOperation.ExecuteAsync(_serviceProvider, token);

                    dumpFilePath = result.Result.Value;
                  
                }
                catch (Exception ex)
                {
                    throw new CollectionRuleActionException(ex);
                }
            }

            return new CollectionRuleActionResult()
            {
                OutputValues = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    { "EgressPath", dumpFilePath }
                }
            };
        }
    }
}
