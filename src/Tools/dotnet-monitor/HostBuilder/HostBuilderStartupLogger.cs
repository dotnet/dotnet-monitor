// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class HostBuilderStartupLogger :
        IStartupLogger
    {
        private readonly HostBuilderContext _builderContext;
        private readonly ILogger _logger;

        public HostBuilderStartupLogger(
            HostBuilderContext builderContext,
            ILogger<Startup> logger)
        {
            _builderContext = builderContext;
            _logger = logger;
        }

        public void Log()
        {
            if (_builderContext.Properties.TryGetValue(HostBuilderResults.ResultKey, out object? resultsObject))
            {
                if (resultsObject is HostBuilderResults hostBuilderResults)
                {
                    foreach (string message in hostBuilderResults.Warnings)
                    {
                        _logger.LogWarning(message);
                    }
                }
            }
        }
    }
}
