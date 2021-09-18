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
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Utils = Microsoft.Diagnostics.Monitoring.WebApi.Utilities;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions
{
    internal sealed class CollectDumpAction : ICollectionRuleAction<CollectDumpOptions>
    {
        private readonly IDumpService _dumpService;
        private readonly IServiceProvider _serviceProvider;

        internal const string EgressPathOutputValueName = "EgressPath";

        public CollectDumpAction(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _dumpService = serviceProvider.GetRequiredService<IDumpService>();
        }

        public async Task<CollectionRuleActionResult> ExecuteAsync(CollectDumpOptions options, IProcessInfo processInfo, CancellationToken token)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (processInfo == null)
            {
                throw new ArgumentNullException(nameof(processInfo));
            }

            DumpType dumpType = options.Type.GetValueOrDefault(CollectDumpOptionsDefaults.Type);
            string egressProvider = options.Egress;

            string dumpFileName = Utils.GenerateDumpFileName();

            string dumpFilePath = string.Empty;

            ValidationContext context = new(options, _serviceProvider, items: null);
            Validator.ValidateObject(options, context, validateAllProperties: true);

            KeyValueLogScope scope = Utils.CreateArtifactScope(Utils.ArtifactType_Dump, processInfo);

            try
            {
                EgressOperation egressOperation = new EgressOperation(
                    token => _dumpService.DumpAsync(processInfo, dumpType, token),
                    egressProvider,
                    dumpFileName,
                    processInfo,
                    ContentTypes.ApplicationOctetStream,
                    scope);

                ExecutionResult<EgressResult> result = await egressOperation.ExecuteAsync(_serviceProvider, token);

                dumpFilePath = result.Result.Value;
            }
            catch (Exception ex)
            {
                throw new CollectionRuleActionException(ex);
            }

            return new CollectionRuleActionResult()
            {
                OutputValues = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    { EgressPathOutputValueName, dumpFilePath }
                }
            };
        }
    }
}
