// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Exceptions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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

        private sealed class CollectTraceAction :
            CollectionRuleActionBase<CollectTraceOptions>
        {
            private readonly IServiceProvider _serviceProvider;
            private readonly IOptionsMonitor<GlobalCounterOptions> _counterOptions;
            private readonly OperationTrackerService _operationTrackerService;

            public CollectTraceAction(IServiceProvider serviceProvider, IEndpointInfo endpointInfo, CollectTraceOptions options)
                : base(endpointInfo, options)
            {
                _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
                _counterOptions = _serviceProvider.GetRequiredService<IOptionsMonitor<GlobalCounterOptions>>();
                _operationTrackerService = _serviceProvider.GetRequiredService<OperationTrackerService>();
            }

            protected override async Task<CollectionRuleActionResult> ExecuteCoreAsync(
                TaskCompletionSource<object> startCompletionSource,
                CancellationToken token)
            {
                TimeSpan duration = Options.Duration.GetValueOrDefault(TimeSpan.Parse(CollectTraceOptionsDefaults.Duration));
                string egressProvider = Options.Egress;

                MonitoringSourceConfiguration configuration;

                if (Options.Profile.HasValue)
                {
                    TraceProfile profile = Options.Profile.Value;
                    float metricsIntervalSeconds = _counterOptions.CurrentValue.GetIntervalSeconds();

                    configuration = TraceUtilities.GetTraceConfiguration(profile, metricsIntervalSeconds);
                }
                else
                {
                    EventPipeProvider[] optionsProviders = Options.Providers.ToArray();
                    bool requestRundown = Options.RequestRundown.GetValueOrDefault(CollectTraceOptionsDefaults.RequestRundown);
                    int bufferSizeMegabytes = Options.BufferSizeMegabytes.GetValueOrDefault(CollectTraceOptionsDefaults.BufferSizeMegabytes);

                    configuration = TraceUtilities.GetTraceConfiguration(optionsProviders, requestRundown, bufferSizeMegabytes);
                }

                string fileName = TraceUtilities.GenerateTraceFileName(EndpointInfo);

                KeyValueLogScope scope = Utils.CreateArtifactScope(Utils.ArtifactType_Trace, EndpointInfo);

                EgressOperation egressOperation = new EgressOperation(
                    async (outputStream, token) =>
                    {
                        using IDisposable operationRegistration = _operationTrackerService.Register(EndpointInfo);
                        await TraceUtilities.CaptureTraceAsync(startCompletionSource, EndpointInfo, configuration, duration, outputStream, token);
                    },
                    egressProvider,
                    fileName,
                    EndpointInfo,
                    ContentTypes.ApplicationOctetStream,
                    scope);

                ExecutionResult<EgressResult> result = await egressOperation.ExecuteAsync(_serviceProvider, token);
                if (null != result.Exception)
                {
                    throw new CollectionRuleActionException(result.Exception);
                }
                string traceFilePath = result.Result.Value;

                return new CollectionRuleActionResult()
                {
                    OutputValues = new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        { CollectionRuleActionConstants.EgressPathOutputValueName, traceFilePath }
                    }
                };
            }
        }
    }
}
