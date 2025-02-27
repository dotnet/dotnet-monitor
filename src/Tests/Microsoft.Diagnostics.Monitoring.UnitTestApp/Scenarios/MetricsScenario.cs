// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Constants = Microsoft.Diagnostics.Monitoring.TestCommon.LiveMetricsTestConstants;

namespace Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios
{
    internal static class MetricsScenario
    {
        public static Command Command()
        {
            Command command = new(TestAppScenarios.Metrics.Name);
            command.SetAction(ExecuteAsync);
            return command;
        }

        public static Task<int> ExecuteAsync(ParseResult result, CancellationToken token)
        {
            return ScenarioHelpers.RunScenarioAsync(async logger =>
            {
                Random rd = new();

                Meter meter1 = new Meter(Constants.ProviderName1, "1.0.0");
                _ = meter1.CreateObservableCounter(Constants.CounterName, () => 1);
                _ = meter1.CreateObservableGauge<int>(Constants.GaugeName, () => rd.Next(1, 100));
                Histogram<int> histogram1 = meter1.CreateHistogram<int>(Constants.HistogramName1);
                Histogram<int> histogram2 = meter1.CreateHistogram<int>(Constants.HistogramName2);

                Meter meter2 = new Meter(Constants.ProviderName2, "1.0.0");
                Counter<int> counter2 = meter2.CreateCounter<int>(Constants.CounterName);

                var meterTags = new Dictionary<string, object>
                {
                    { Constants.MeterMetadataKey, Constants.MeterMetadataValue }
                };

                var instrumentTags = new Dictionary<string, object>
                {
                    { Constants.InstrumentMetadataKey, Constants.InstrumentMetadataValue }
                };

#if NET8_0_OR_GREATER
                Meter meter3 = new Meter(Constants.ProviderName3, "1.0.0", meterTags);
                Counter<int> counter3 = meter3.CreateCounter<int>(Constants.CounterName, null, null, instrumentTags);
#endif

                var metadata = new Dictionary<string, object>
                {
                    { Constants.MetadataKey, Constants.MetadataValue }
                };

                Task continueCommand = Task.Run(() => ScenarioHelpers.WaitForCommandAsync(TestAppScenarios.Metrics.Commands.Continue, logger));

                while (!continueCommand.IsCompleted)
                {
                    for (int i = 1; i <= 100; ++i)
                    {
                        histogram1.Record(i);
                        histogram2.Record(i, metadata.ToArray());
                    }

                    counter2.Add(1);

#if NET8_0_OR_GREATER
                    counter3.Add(1, metadata.ToArray());
#endif

                    await Task.Delay(100);
                }

                return 0;
            }, token);
        }
    }
}
