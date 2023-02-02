// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal class StartupLoggingHostedService :
        IHostedService
    {
        private readonly IEnumerable<IStartupLogger> _loggers;

        public StartupLoggingHostedService(IEnumerable<IStartupLogger> loggers)
        {
            _loggers = loggers;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            foreach (IStartupLogger logger in _loggers)
            {
                logger.Log();
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
