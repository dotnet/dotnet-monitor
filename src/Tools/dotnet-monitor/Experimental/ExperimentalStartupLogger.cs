// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class ExperimentalStartupLogger :
        IStartupLogger
    {
        private readonly ILogger _logger;

        public ExperimentalStartupLogger(ILogger<Startup> logger)
        {
            _logger = logger;
        }

        public void Log()
        {
            if (ExperimentalFlags.IsCallStacksEnabled)
            {
                _logger.ExperimentalFeatureEnabled(Strings.FeatureName_CallStacks);
            }
        }
    }
}
