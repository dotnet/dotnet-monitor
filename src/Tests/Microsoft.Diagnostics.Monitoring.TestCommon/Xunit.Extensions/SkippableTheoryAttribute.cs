using Xunit.Sdk;

namespace Xunit.Extensions
{
    [XunitTestCaseDiscoverer("Xunit.Extensions.SkippableTheoryDiscoverer", "Microsoft.Diagnostics.Monitoring.TestCommon")]
    public class SkippableTheoryAttribute : TheoryAttribute { }
}
