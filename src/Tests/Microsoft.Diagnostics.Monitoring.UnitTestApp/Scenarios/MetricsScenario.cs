﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading.Tasks;
using Constants = Microsoft.Diagnostics.Monitoring.TestCommon.LiveMetricsTestConstants;

namespace Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios
{
    internal static class MetricsScenario
    {
        public static Command Command()
        {
            Command command = new(TestAppScenarios.Metrics.Name);
            command.SetHandler(ExecuteAsync);
            return command;
        }

        public static async Task ExecuteAsync(InvocationContext context)
        {
            context.ExitCode = await ScenarioHelpers.RunScenarioAsync(async logger =>
            {
                Random rd = new();

                Meter meter1 = new Meter(Constants.ProviderName1, "1.0.0");
                _ = meter1.CreateObservableCounter(Constants.CounterName, () => 1);
                _ = meter1.CreateObservableGauge<int>(Constants.GaugeName, () => rd.Next(1, 100));
                Histogram<int> histogram1 = meter1.CreateHistogram<int>(Constants.HistogramName1);
                Histogram<int> histogram2 = meter1.CreateHistogram<int>(Constants.HistogramName2);

                Meter meter2 = new Meter(Constants.ProviderName2, "1.0.0");
                Counter<int> counter2 = meter2.CreateCounter<int>(Constants.CounterName);

                var metadata = new Dictionary<string, object>
                {
                    { Constants.MetadataKey, Constants.MetadataValue }
                };

                Task continueCommand = Task.Run(() => ScenarioHelpers.WaitForCommandAsync(TestAppScenarios.Metrics.Commands.Continue, logger));

                while (!continueCommand.IsCompleted)
                {
                    for (int i = 0; i < 20; ++i)
                    {
                        histogram1.Record(rd.Next(5000));
                        histogram2.Record(rd.Next(5000), metadata.ToArray());
                    }

                    counter2.Add(1);

                    await Task.Delay(100);
                }

                return 0;
            }, context.GetCancellationToken());
        }
    }
}
