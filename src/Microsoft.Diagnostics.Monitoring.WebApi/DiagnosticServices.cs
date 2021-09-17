// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        private readonly IEndpointInfoSource _endpointInfoSource;
        private readonly IOptionsMonitor<ProcessFilterOptions> _defaultProcessOptions;

        public DiagnosticServices(IEndpointInfoSource endpointInfoSource,
            IOptionsMonitor<ProcessFilterOptions> defaultProcessMonitor)
        {
            _endpointInfoSource = endpointInfoSource;
            _defaultProcessOptions = defaultProcessMonitor;
        }

        public async Task<IEnumerable<IEndpointInfo>> GetProcessesAsync(DiagProcessFilter processFilterConfig, CancellationToken token)
        {
            try
            {
                IEnumerable<IEndpointInfo> processes = await _endpointInfoSource.GetEndpointInfoAsync(token);

                if (processFilterConfig != null)
                {
                    processes = processes.Where(p => processFilterConfig.Filters.All(c => c.MatchFilter(p)));
                }

                return processes.ToArray();
            }
            catch (UnauthorizedAccessException)
            {
                throw new InvalidOperationException(Strings.ErrorMessage_ProcessEnumeratuinFailed);
            }
        }

        public Task<IEndpointInfo> GetProcessAsync(ProcessKey? processKey, CancellationToken token)
        {
            DiagProcessFilter filterOptions = null;
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

        private async Task<IEndpointInfo> GetProcessAsync(DiagProcessFilter processFilterConfig, CancellationToken token)
        {
            //Short circuit when we are missing default process config
            if (!processFilterConfig.Filters.Any())
            {
                throw new InvalidOperationException(Strings.ErrorMessage_NoDefaultProcessConfig);
            }
            IEnumerable<IEndpointInfo> matchingProcesses = await GetProcessesAsync(processFilterConfig, token);

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
