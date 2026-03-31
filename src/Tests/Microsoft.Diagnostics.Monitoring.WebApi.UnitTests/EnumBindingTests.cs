// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.WebApi.UnitTests
{
    public class EnumBindingTests
    {
        [Theory]
        [InlineData("withheap", DumpType.WithHeap)]
        [InlineData("MINI", DumpType.Mini)]
        [InlineData("Triage", DumpType.Triage)]
        public void EnumBinding_TryParse_IsCaseInsensitive_ForSingleValues(string value, DumpType expected)
        {
            bool success = EnumBinding<DumpType>.TryParse(value, provider: null, out EnumBinding<DumpType> result);

            Assert.True(success);
            Assert.Equal(expected, result.Value);
        }

        [Theory]
        [InlineData("cpu,http", TraceProfile.Cpu | TraceProfile.Http)]
        [InlineData("CPU,METRICS", TraceProfile.Cpu | TraceProfile.Metrics)]
        [InlineData("logs,gccollect", TraceProfile.Logs | TraceProfile.GcCollect)]
        public void EnumBinding_TryParse_IsCaseInsensitive_ForFlagsValues(string value, TraceProfile expected)
        {
            bool success = EnumBinding<TraceProfile>.TryParse(value, provider: null, out EnumBinding<TraceProfile> result);

            Assert.True(success);
            Assert.Equal(expected, result.Value);
        }

        [Fact]
        public void EnumBinding_ImplicitConversions_Work()
        {
            EnumBinding<LogLevel> binding = LogLevel.Warning;
            LogLevel value = binding;

            Assert.Equal(LogLevel.Warning, value);
        }
    }
}
