// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Controllers;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Exceptions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Extensions.DependencyInjection;
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
        //private readonly IDumpService _dumpService;
        private readonly IServiceProvider _serviceProvider;

        public CollectLogsAction(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            //_dumpService = serviceProvider.GetRequiredService<IDumpService>();
        }

        public Task<CollectionRuleActionResult> ExecuteAsync(CollectLogsOptions options, IEndpointInfo endpointInfo, CancellationToken token)
        {
            TimeSpan duration = options.Duration.GetValueOrDefault(TimeSpan.Parse(CollectLogsOptionsDefaults.Duration));
            bool useAppFilters = options.UseAppFilters.GetValueOrDefault(CollectLogsOptionsDefaults.UseAppFilters);
            Extensions.Logging.LogLevel defaultLevel = options.DefaultLevel.GetValueOrDefault(CollectLogsOptionsDefaults.DefaultLevel);
            string egressProvider = options.Egress;

            var settings = new EventLogsPipelineSettings()
            {
                Duration = duration
            };

            settings.LogLevel = defaultLevel;
            settings.UseAppFilters = useAppFilters;

            string logsFilePath = await StartLogs(endpointInfo, settings, egressProvider);

            return new CollectionRuleActionResult()
            {
                OutputValues = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    { "EgressPath", logsFilePath }
                }
            };
        }

        private Task<string> StartLogs(
            IEndpointInfo endpointInfo,
            EventLogsPipelineSettings settings,
            string egressProvider)
        {
            LogFormat format = ComputeLogFormat(Request.GetTypedHeaders().Accept); // We have no HTTP request, so this might be a problem...? Do we want another parameter for it, or just choose a default?
            if (format == LogFormat.None)
            {
                throw new ...
            }

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
        }
    }
}
