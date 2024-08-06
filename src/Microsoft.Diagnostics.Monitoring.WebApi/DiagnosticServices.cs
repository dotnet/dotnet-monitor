// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal sealed class DiagnosticServices : IDiagnosticServices
    {
        private readonly IEndpointInfoSourceInternal _endpointInfoSource;
        private readonly IOptionsMonitor<ProcessFilterOptions> _defaultProcessOptions;
        private readonly ILogger _logger;

        public DiagnosticServices(IEndpointInfoSource endpointInfoSource,
            IOptionsMonitor<ProcessFilterOptions> defaultProcessMonitor,
            ILogger<DiagnosticServices> logger)
        {
            _endpointInfoSource = (IEndpointInfoSourceInternal)endpointInfoSource;
            _defaultProcessOptions = defaultProcessMonitor;
            _logger = logger;
        }

        public async Task<IEnumerable<IProcessInfo>> GetProcessesAsync(DiagProcessFilter? processFilterConfig, CancellationToken token)
        {
            IEnumerable<IProcessInfo> processes = [];

            try
            {
                using CancellationTokenSource extendedInfoCancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
                IList<Task<IProcessInfo?>> processInfoTasks = new List<Task<IProcessInfo?>>();
                foreach (IEndpointInfo endpointInfo in await _endpointInfoSource.GetEndpointInfoAsync(token))
                {
                    // CONSIDER: Can this processing be pushed into the IEndpointInfoSource implementation and cached
                    // so that extended process information doesn't have to be recalculated for every call. This would be
                    // useful for:
                    // - .NET Core 3.1 processes, which require issuing a brief event pipe session to get the process command
                    //   line information and parse out the process name
                    // - Caching entrypoint information (when that becomes available).
                    processInfoTasks.Add(Task.Run(
                        async () =>
                        {
                            try
                            {
                                return await ProcessInfoImpl.FromEndpointInfoAsync(
                                    endpointInfo,
                                    extendedInfoCancellation.Token);
                            }
                            catch (Exception ex) when (!(ex is OperationCanceledException))
                            {
                                // This likely occurs when the process exits after it was discovered
                                // and before an IProcessInfo could be created for it.
                                _logger.DiagnosticRequestFailed(endpointInfo.ProcessId, ex);
                            }
                            return null;
                        },
                        token));
                }

                // FromEndpointInfoAsync can fill in the command line for .NET Core 3.1 processes by invoking the
                // event pipe and capturing the ProcessInfo event. Time-box this operation with the cancellation token
                // so that getting the process list does not take a long time or wait indefinitely.
                extendedInfoCancellation.CancelAfter(ProcessInfoImpl.ExtendedProcessInfoTimeout);

                await Task.WhenAll(processInfoTasks);

                processes = processInfoTasks
                    .Where(t => t.Result != null)
                    .Select(t => t.Result)
                    .Cast<IProcessInfo>();
            }
            catch (UnauthorizedAccessException)
            {
                throw new InvalidOperationException(Strings.ErrorMessage_ProcessEnumerationFailed);
            }

            if (processFilterConfig != null)
            {
                processes = processes.Where(p => processFilterConfig.Filters.All(c => c.MatchFilter(p)));
            }

            return processes.ToArray();
        }

        public Task<IProcessInfo> GetProcessAsync(ProcessKey? processKey, CancellationToken token)
        {
            DiagProcessFilter filterOptions;
            if (processKey.HasValue)
            {
                filterOptions = DiagProcessFilter.FromProcessKey(processKey.Value);
            }
            else
            {
                filterOptions = DiagProcessFilter.FromConfiguration(_defaultProcessOptions.CurrentValue);
            }

            return GetProcessAsync(filterOptions, token);
        }

        private async Task<IProcessInfo> GetProcessAsync(DiagProcessFilter processFilterConfig, CancellationToken token)
        {
            //Short circuit when we are missing default process config
            if (!processFilterConfig.Filters.Any())
            {
                throw new InvalidOperationException(Strings.ErrorMessage_NoDefaultProcessConfig);
            }
            IEnumerable<IProcessInfo> matchingProcesses = await GetProcessesAsync(processFilterConfig, token);

            switch (matchingProcesses.Count())
            {
                case 0:
                    throw new ArgumentException(Strings.ErrorMessage_NoTargetProcess);
                case 1:
                    return matchingProcesses.First();
                default:
                    throw new ArgumentException(Strings.ErrorMessage_MultipleTargetProcesses);
            }
        }
    }
}
