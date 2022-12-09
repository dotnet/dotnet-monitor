// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Diagnostics.Monitoring.WebApi.UnitTests
{
    public class PrometheusDataModelTests
    {
        [Theory()]
        [InlineData("System.Runtime", "cpu-usage", "%", "systemruntime_cpu_usage_ratio")]
        [InlineData("System.Runtime", "working-set", "MB", "systemruntime_working_set_bytes")]
        [InlineData("System.Runtime", "gc-heap-size", "MB", "systemruntime_gc_heap_size_bytes")]
        [InlineData("System.Runtime", "gen-0-gc-count", "count", "systemruntime_gen_0_gc_count")]
        [InlineData("System.Runtime", "gen-1-gc-count", "count", "systemruntime_gen_1_gc_count")]
        [InlineData("System.Runtime", "gen-2-gc-count", "count", "systemruntime_gen_2_gc_count")]
        [InlineData("System.Runtime", "threadpool-thread-count", "", "systemruntime_threadpool_thread_count")]
        [InlineData("System.Runtime", "monitor-lock-contention-count", "count", "systemruntime_monitor_lock_contention_count")]
        [InlineData("System.Runtime", "threadpool-queue-length", "", "systemruntime_threadpool_queue_length")]
        [InlineData("System.Runtime", "threadpool-completed-items-count", "count", "systemruntime_threadpool_completed_items_count")]
        [InlineData("System.Runtime", "alloc-rate", "B", "systemruntime_alloc_rate_bytes")]
        [InlineData("System.Runtime", "active-timer-count", "", "systemruntime_active_timer_count")]
        [InlineData("System.Runtime", "gc-fragmentation", "%", "systemruntime_gc_fragmentation_ratio")]
        [InlineData("System.Runtime", "gc-committed", "MB", "systemruntime_gc_committed_bytes")]
        [InlineData("System.Runtime", "exception-count", "count", "systemruntime_exception_count")]
        [InlineData("System.Runtime", "time-in-gc", "%", "systemruntime_time_in_gc_ratio")]
        [InlineData("System.Runtime", "gen-0-size", "B", "systemruntime_gen_0_size_bytes")]
        [InlineData("System.Runtime", "gen-1-size", "B", "systemruntime_gen_1_size_bytes")]
        [InlineData("System.Runtime", "gen-2-size", "B", "systemruntime_gen_2_size_bytes")]
        [InlineData("System.Runtime", "loh-size", "B", "systemruntime_loh_size_bytes")]
        [InlineData("System.Runtime", "poh-size", "B", "systemruntime_poh_size_bytes")]
        [InlineData("System.Runtime", "assembly-count", "", "systemruntime_assembly_count")]
        [InlineData("System.Runtime", "il-bytes-jitted", "B", "systemruntime_il_bytes_jitted_bytes")]
        [InlineData("System.Runtime", "methods-jitted-count", "", "systemruntime_methods_jitted_count")]
        [InlineData("System.Runtime", "time-in-jit", "ms", "systemruntime_time_in_jit_ms")]
        [InlineData("Microsoft.AspNetCore.Hosting", "requests-per-second", "count", "microsoftaspnetcorehosting_requests_per_second")]
        [InlineData("Microsoft.AspNetCore.Hosting", "total-requests", "", "microsoftaspnetcorehosting_total_requests")]
        [InlineData("Microsoft.AspNetCore.Hosting", "current-requests", "", "microsoftaspnetcorehosting_current_requests")]
        [InlineData("Microsoft.AspNetCore.Hosting", "failed-requests", "", "microsoftaspnetcorehosting_failed_requests")]
        [InlineData("Test-Provider", "Test-Metric", "b", "testprovider_Test_Metric_bytes")]
        [InlineData("!@#", "#@#", "b", "______bytes")]
        [InlineData("Asp-Net-Provider", "Requests!Received", "0$customs", "aspnetprovider_Requests_Received___customs")]
        [InlineData("a", "b", null, "a_b")]
        [InlineData("UnicodeάήΰLetter", "Unicode\u0befDigit", null, "unicodeletter_Unicode_Digit")]
        public void TestGetPrometheusNormalizedName(string metricProvider, string metricName, string metricUnit, string expectedName)
        {
            var normalizedMetricName = PrometheusDataModel.GetPrometheusNormalizedName(metricProvider, metricName, metricUnit);
            Assert.Equal(expectedName, normalizedMetricName);
        }

        [Theory()]
        [InlineData("", 225, "225")]
        [InlineData("MB", 9, "9000000")]
        [InlineData("mb", 112, "112000000")]
        [InlineData("B", 48, "48")]
        [InlineData("B", 178936, "178936")]
        [InlineData("b", 461142, "461142")]
        [InlineData("ms", 0, "0")]
        [InlineData("", 4, "4")]
        [InlineData("count", 10, "10")]
        [InlineData("%", 2, "2")]
        [InlineData("%", 0.691783039570667, "0.691783039570667")]
        [InlineData("%", 0, "0")]
        public void TestGetPrometheusNormalizedValue(string metricUnit, double metricValue, string expectedValue)
        {
            var normalizedValue = PrometheusDataModel.GetPrometheusNormalizedValue(metricUnit, metricValue);
            Assert.Equal(normalizedValue, expectedValue);
        }

        [Theory()]
        [InlineData("key", "value", "key=\"value\"")]
        [InlineData("key*1", "value*1", "key_1=\"value*1\"")]
        [InlineData("key 1", "value 1", "key_1=\"value 1\"")]
        [InlineData("&*()", "Test\nice", "____=\"Test\\nice\"")]
        [InlineData("", "Test\\nice", "=\"Test\\\\nice\"")]
        [InlineData("Test\\test", "Test\\test", "Test_test=\"Test\\\\test\"")]
        [InlineData("UnicodeάήΰLetter", "Test\\\nice", "Unicode___Letter=\"Test\\\\\\nice\"")]
        [InlineData("ά", "Test\"quotes\"", "_=\"Test\\\"quotes\\\"\"")]
        [InlineData("_key", "Test\\\"quotes\\\"", "_key=\"Test\\\\\\\"quotes\\\\\\\"\"")]
        public void TestGetPrometheusNormalizedMetadataValue(string key, string value, string expectedLabel)
        {
            var normalizedLabel = PrometheusDataModel.GetPrometheusNormalizedLabel(key, value);
            Assert.Equal(expectedLabel, normalizedLabel);
        }
    }
}
