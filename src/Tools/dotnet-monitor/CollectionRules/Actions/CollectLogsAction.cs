// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Utils = Microsoft.Diagnostics.Monitoring.WebApi.Utilities;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions
{
    internal sealed class CollectLogsAction :
        ICollectionRuleAction<CollectLogsOptions>
    {
        private readonly IServiceProvider _serviceProvider;

        public CollectLogsAction(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<CollectionRuleActionResult> ExecuteAsync(CollectLogsOptions options, IEndpointInfo endpointInfo, CancellationToken token)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (endpointInfo == null)
            {
                throw new ArgumentNullException(nameof(endpointInfo));
            }

            ValidationContext context = new(options, _serviceProvider, items: null);
            Validator.ValidateObject(options, context, validateAllProperties: true);

            TimeSpan duration = options.Duration.GetValueOrDefault(TimeSpan.Parse(CollectLogsOptionsDefaults.Duration));
            bool useAppFilters = options.UseAppFilters.GetValueOrDefault(CollectLogsOptionsDefaults.UseAppFilters);
            LogLevel defaultLevel = options.DefaultLevel.GetValueOrDefault(CollectLogsOptionsDefaults.DefaultLevel);
            string egressProvider = options.Egress;
            LogFormat logFormat = options.Format.GetValueOrDefault(CollectLogsOptionsDefaults.Format);

            var settings = new EventLogsPipelineSettings()
            {
                Duration = duration
            };

            settings.LogLevel = defaultLevel;
            settings.UseAppFilters = useAppFilters;

            string logsFilePath = await StartLogs(endpointInfo, settings, egressProvider, logFormat, token);

            return new CollectionRuleActionResult()
            {
                OutputValues = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    { "EgressPath", logsFilePath }
                }
            };
        }

        // Move this method to Utilities and share it instead of copying it
        private async Task<string> StartLogs(
            IEndpointInfo endpointInfo,
            EventLogsPipelineSettings settings,
            string egressProvider,
            LogFormat format,
            CancellationToken token)
        {
            string fileName = FormattableString.Invariant($"{Utils.GetFileNameTimeStampUtcNow()}_{endpointInfo.ProcessId}.txt");
            string contentType = ContentTypes.TextEventStream;

            if (format == LogFormat.EventStream)
            {
                contentType = ContentTypes.TextEventStream;
            }
            else if (format == LogFormat.NDJson)
            {
                contentType = ContentTypes.ApplicationNdJson;
            }
            else if (format == LogFormat.JsonSequence)
            {
                contentType = ContentTypes.ApplicationJsonSequence;
            }

            Func<Stream, CancellationToken, Task> action = async (outputStream, token) =>
            {
                using var loggerFactory = new LoggerFactory();

                loggerFactory.AddProvider(new StreamingLoggerProvider(outputStream, format, logLevel: null));

                var client = new DiagnosticsClient(endpointInfo.Endpoint);

                await using EventLogsPipeline pipeline = new EventLogsPipeline(client, settings, loggerFactory);
                await pipeline.RunAsync(token);
            };

            KeyValueLogScope scope = Utils.GetScope(Utils.ArtifactType_Logs, endpointInfo);

            EgressOperation egressOperation = new EgressOperation(
                action,
                egressProvider,
                fileName,
                endpointInfo,
                contentType,
                scope);

            ExecutionResult<EgressResult> result = await egressOperation.ExecuteAsync(_serviceProvider, token);

            return result.Result.Value;
        }
    }
}
