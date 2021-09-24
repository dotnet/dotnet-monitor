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
using System.Threading;
using System.Threading.Tasks;
using Utils = Microsoft.Diagnostics.Monitoring.WebApi.Utilities;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions
{
    internal sealed class CollectTraceActionFactory :
        ICollectionRuleActionFactory<CollectTraceOptions>
    {
        private readonly IServiceProvider _serviceProvider;

        public ICollectionRuleAction Create(IEndpointInfo endpointInfo, CollectTraceOptions options)
        {
            if (null == options)
            {
                throw new ArgumentNullException(nameof(options));
            }

            ValidationContext context = new(options, _serviceProvider, items: null);
            Validator.ValidateObject(options, context, validateAllProperties: true);

            return new CollectTraceAction(_serviceProvider, endpointInfo, options);
        }

        public CollectTraceActionFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        internal sealed class CollectTraceAction :
            CollectionRuleActionBase<CollectTraceOptions>
        {
            private readonly IServiceProvider _serviceProvider;

            public CollectTraceAction(IServiceProvider serviceProvider, IEndpointInfo endpointInfo, CollectTraceOptions options)
                : base(endpointInfo, options)
            {
                _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            }

            protected override async Task<CollectionRuleActionResult> ExecuteCoreAsync(
                TaskCompletionSource<object> startCompletionSource,
                CancellationToken token)
            {
                TimeSpan duration = Options.Duration.GetValueOrDefault(TimeSpan.Parse(CollectTraceOptionsDefaults.Duration));
                string egressProvider = Options.Egress;

                string traceFilePath = string.Empty;

                if (null != Options.Profile && null == Options.Providers)
                {
                    TraceProfile profile = Options.Profile.Value;
                    int metricsIntervalSeconds = Options.MetricsIntervalSeconds.GetValueOrDefault(CollectTraceOptionsDefaults.MetricsIntervalSeconds);

                    var aggregateConfiguration = Utils.GetTraceConfiguration(profile, metricsIntervalSeconds);

                    traceFilePath = await StartTrace(startCompletionSource, EndpointInfo, aggregateConfiguration, duration, egressProvider, token);
                }
                else if (null != Options.Providers && null == Options.Profile)
                {
                    EventPipeProvider[] optionsProviders = Options.Providers.ToArray();
                    bool requestRundown = Options.RequestRundown.GetValueOrDefault(CollectTraceOptionsDefaults.RequestRundown);
                    int bufferSizeMegabytes = Options.BufferSizeMegabytes.GetValueOrDefault(CollectTraceOptionsDefaults.BufferSizeMegabytes);

                    var traceConfiguration = Utils.GetCustomTraceConfiguration(optionsProviders, requestRundown, bufferSizeMegabytes);

                    traceFilePath = await StartTrace(startCompletionSource, EndpointInfo, traceConfiguration, duration, egressProvider, token);
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
                TaskCompletionSource<object> startCompletionSource,
                IEndpointInfo endpointInfo,
                MonitoringSourceConfiguration configuration,
                TimeSpan duration,
                string egressProvider,
                CancellationToken token)
            {
                string fileName = Utils.GenerateTraceFileName(endpointInfo);

                KeyValueLogScope scope = Utils.CreateArtifactScope(Utils.ArtifactType_Trace, endpointInfo);

                EgressOperation egressOperation = new EgressOperation(
                    (outputStream, token) =>
                    {
                        startCompletionSource.TrySetResult(null);
                        return Utils.GetTraceAction(EndpointInfo, configuration, duration, outputStream, token);
                    },
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
}