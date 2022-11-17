// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Extensions.Logging;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics.Metrics;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios
{
#if NET6_0_OR_GREATER
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
                await ScenarioHelpers.WaitForCommandAsync(TestAppScenarios.Metrics.Commands.Continue, logger);

                Random rd = new();

                Meter meter1 = new Meter("P1", "1.0.0");
                Counter<int> counter1 = meter1.CreateCounter<int>("test-counter");
                Histogram<int> histogram1 = meter1.CreateHistogram<int>("test-histogram");
                _ = meter1.CreateObservableGauge<int>("test-gauge", () => rd.Next(1, 100));

                Meter meter2 = new Meter("P2", "1.0.0");
                Histogram<int> histogram2 = meter2.CreateHistogram<int>("test-histogram");

                for (int index = 0; index < 20; ++index)
                {
                    await Task.Delay(100);
                    counter1.Add(1);

                    for (int i = 0; i < 20; ++i)
                    {
                        histogram1.Record(rd.Next(5000));
                        histogram2.Record(rd.Next(5000));
                    }
                }

                await ScenarioHelpers.WaitForCommandAsync(TestAppScenarios.Metrics.Commands.Continue, logger);

                return 0;
            }, context.GetCancellationToken());
        }
    }
#endif
}
