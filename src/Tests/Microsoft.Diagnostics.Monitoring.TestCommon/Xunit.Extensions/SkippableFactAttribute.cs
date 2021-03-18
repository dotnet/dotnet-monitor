using Xunit.Sdk;

namespace Xunit.Extensions
{
    [XunitTestCaseDiscoverer("Xunit.Extensions.SkippableFactDiscoverer", "Microsoft.Diagnostics.Monitoring.TestCommon")]
    public class SkippableFactAttribute : FactAttribute { }
}
