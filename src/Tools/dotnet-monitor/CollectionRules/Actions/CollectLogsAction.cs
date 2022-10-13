// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Exceptions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Utils = Microsoft.Diagnostics.Monitoring.WebApi.Utilities;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions
{
    internal sealed class CollectLogsActionFactory :
        ICollectionRuleActionFactory<CollectLogsOptions>
    {
        private readonly IServiceProvider _serviceProvider;

        public CollectLogsActionFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public ICollectionRuleAction Create(IEndpointInfo endpointInfo, CollectLogsOptions options)
        {
            if (null == options)
            {
                throw new ArgumentNullException(nameof(options));
            }

            ValidationContext context = new(options, _serviceProvider, items: null);
            Validator.ValidateObject(options, context, validateAllProperties: true);

            return new CollectLogsAction(_serviceProvider, endpointInfo, options);
        }

        private sealed class CollectLogsAction :
            CollectionRuleActionBase<CollectLogsOptions>
        {
            private readonly IServiceProvider _serviceProvider;

            public CollectLogsAction(IServiceProvider serviceProvider, IEndpointInfo endpointInfo, CollectLogsOptions options)
                : base(endpointInfo, options)
            {
                _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            }

            protected override async Task<CollectionRuleActionResult> ExecuteCoreAsync(
                TaskCompletionSource<object> startCompletionSource,
                CollectionRuleMetadata collectionRuleMetadata,
                CancellationToken token)
            {
                TimeSpan duration = Options.Duration.GetValueOrDefault(TimeSpan.Parse(CollectLogsOptionsDefaults.Duration));
                bool useAppFilters = Options.UseAppFilters.GetValueOrDefault(CollectLogsOptionsDefaults.UseAppFilters);
                LogLevel defaultLevel = Options.DefaultLevel.GetValueOrDefault(CollectLogsOptionsDefaults.DefaultLevel);
                Dictionary<string, LogLevel?> filterSpecs = Options.FilterSpecs;
                string egressProvider = Options.Egress;
                LogFormat logFormat = Options.Format.GetValueOrDefault(CollectLogsOptionsDefaults.Format);

                var settings = new EventLogsPipelineSettings()
                {
                    Duration = duration,
                    LogLevel = defaultLevel,
                    UseAppFilters = useAppFilters,
                    FilterSpecs = filterSpecs
                };

                string fileName = LogsUtilities.GenerateLogsFileName(EndpointInfo);
                string contentType = LogsUtilities.GetLogsContentType(logFormat);

                KeyValueLogScope scope = Utils.CreateArtifactScope(Utils.ArtifactType_Logs, EndpointInfo);

                EgressOperation egressOperation = new EgressOperation(
                    (outputStream, token) => LogsUtilities.CaptureLogsAsync(startCompletionSource, logFormat, EndpointInfo, settings, outputStream, token),
                    egressProvider,
                    fileName,
                    EndpointInfo,
                    contentType,
                    scope,
                    collectionRuleMetadata);

                ExecutionResult<EgressResult> result = await egressOperation.ExecuteAsync(_serviceProvider, token);
                if (null != result.Exception)
                {
                    throw new CollectionRuleActionException(result.Exception);
                }
                string logsFilePath = result.Result.Value;

                return new CollectionRuleActionResult()
                {
                    OutputValues = new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        { CollectionRuleActionConstants.EgressPathOutputValueName, logsFilePath }
                    }
                };
            }
        }
    }
}
