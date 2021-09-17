﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using FastSerialization;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Controllers;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Utils = Microsoft.Diagnostics.Monitoring.WebApi.Utilities;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions
{
    internal sealed class CollectGCDumpAction : ICollectionRuleAction<CollectGCDumpOptions>
    {
        private readonly IServiceProvider _serviceProvider;

        public CollectGCDumpAction(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
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

            string gcdumpFileName = Utils.GenerateGCDumpFileName(endpointInfo);

            string gcdumpFilePath = string.Empty;

            Func<CancellationToken, Task<IFastSerializable>> action = Utils.GetGCHeadDump(endpointInfo);

            KeyValueLogScope scope = Utils.GetScope(Utils.ArtifactType_GCDump, endpointInfo);

            EgressOperation egressOperation = new EgressOperation(
                        Utils.ConvertFastSerializeAction(action),
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
                    { CollectionRuleActionConstants.EgressPathOutputValueName, gcdumpFilePath }
                }
            };
        }
    }
}