using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

#if UNITTEST
namespace Microsoft.Diagnostics.Monitoring.UnitTests.Options
#else
namespace Microsoft.Diagnostics.Monitoring.RestServer
#endif
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
