// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Utils = Microsoft.Diagnostics.Monitoring.WebApi.Utilities;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions
{
    internal sealed class CollectTraceAction : ICollectionRuleAction<CollectTraceOptions>
    {
        private readonly IServiceProvider _serviceProvider;

        public CollectTraceAction(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<CollectionRuleActionResult> ExecuteAsync(CollectTraceOptions options, IEndpointInfo endpointInfo, CancellationToken token)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (endpointInfo == null)
            {
                throw new ArgumentNullException(nameof(endpointInfo));
            }

            TimeSpan duration = options.Duration.GetValueOrDefault(TimeSpan.Parse(CollectTraceOptionsDefaults.Duration));
            string egressProvider = options.Egress;

            ValidationContext context = new(options, _serviceProvider, items: null);
            Validator.ValidateObject(options, context, validateAllProperties: true);

            string traceFilePath = string.Empty;

            if (options.Profile != null && options.Providers == null)
            {
                TraceProfile profile = options.Profile.Value;
                int metricsIntervalSeconds = options.MetricsIntervalSeconds.GetValueOrDefault(CollectTraceOptionsDefaults.MetricsIntervalSeconds);

                var aggregateConfiguration = Utils.GetTraceConfiguration(profile, metricsIntervalSeconds);

                traceFilePath = await StartTrace(endpointInfo, aggregateConfiguration, duration, egressProvider, token);
            }
            else if (options.Providers != null && options.Profile == null)
            {
                EventPipeProvider[] optionsProviders = options.Providers.ToArray();
                bool requestRundown = options.RequestRundown.GetValueOrDefault(CollectTraceOptionsDefaults.RequestRundown);
                int bufferSizeMegabytes = options.BufferSizeMegabytes.GetValueOrDefault(CollectTraceOptionsDefaults.BufferSizeMegabytes);

                var traceConfiguration = Utils.GetCustomTraceConfiguration(optionsProviders, requestRundown, bufferSizeMegabytes);

                traceFilePath = await StartTrace(endpointInfo, traceConfiguration, duration, egressProvider, token);
            }
            else
            {
                throw new ArgumentException("One of the Profile and Providers fields must be provided."); // Temporary message -> put into Strings.resx if we keep the validation
            }

            return new CollectionRuleActionResult()
            {
                OutputValues = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    { CollectionRuleActionConstants.EgressPathOutputValueName, traceFilePath }
                }
            };
        }

        private async Task<string> StartTrace(
            IEndpointInfo endpointInfo,
            MonitoringSourceConfiguration configuration,
            TimeSpan duration,
            string egressProvider,
            CancellationToken token)
        {
            string fileName = Utils.GenerateTraceFileName(endpointInfo);

            Func<Stream, CancellationToken, Task> action = Utils.GetTraceAction(endpointInfo, configuration, duration);

            KeyValueLogScope scope = Utils.CreateArtifactScope(Utils.ArtifactType_Trace, endpointInfo);

            EgressOperation egressOperation = new EgressOperation(
                action,
                egressProvider,
                fileName,
                endpointInfo,
                ContentTypes.ApplicationOctetStream,
                scope);

            ExecutionResult<EgressResult> result = await egressOperation.ExecuteAsync(_serviceProvider, token);

            string traceFilePath = result.Result.Value;

            return traceFilePath;
        }
    }
}