using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.RestServer
{
    internal class MetricsOptionsDefaults
    {
        public const bool Enabled = true;

        public const int UpdateIntervalSeconds = 10;

        public const int MetricCount = 3;

        public const bool IncludeDefaultProviders = true;

        public const bool AllowInsecureChannelForCustomMetrics = false;
    }
}
