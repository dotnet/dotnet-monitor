// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using System;
using System.CommandLine;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios
{
    /// <summary>
    /// Continuously emits trace events and a unique one on request.
    /// Only stops once an exit request is received.
    /// </summary>
    internal static class TraceEventsScenario
    {
        [EventSource(Name = "TestScenario")]
        private sealed class TestScenarioEventSource : EventSource
        {
            public static TestScenarioEventSource Log { get; } = new TestScenarioEventSource();

            [Event(1)]
            public void RandomNumberGenerated(int number) => WriteEvent(1, number);

            [Event(2, Opcode = EventOpcode.Reply)]
            public void UniqueEvent(string message) => WriteEvent(2, message);
        }

        public static Command Command()
        {
            Command command = new(TestAppScenarios.TraceEvents.Name);
            command.SetAction(ExecuteAsync);
            return command;
        }

        public static Task<int> ExecuteAsync(ParseResult result, CancellationToken token)
        {
            string[] acceptableCommands = new string[]
            {
                TestAppScenarios.TraceEvents.Commands.EmitUniqueEvent,
                TestAppScenarios.TraceEvents.Commands.ShutdownScenario
            };

            return ScenarioHelpers.RunScenarioAsync(async logger =>
            {
                using ManualResetEventSlim stopGeneratingEvents = new(initialState: false);

                Task eventEmitterTask = Task.Run(async () =>
                {
                    Random random = new();
                    while (!stopGeneratingEvents.IsSet)
                    {
                        TestScenarioEventSource.Log.RandomNumberGenerated(random.Next());
                        await Task.Delay(TimeSpan.FromMilliseconds(100), token);
                    }
                }, token);

                while (true)
                {
                    switch (await ScenarioHelpers.WaitForCommandAsync(acceptableCommands, logger))
                    {
                        case TestAppScenarios.TraceEvents.Commands.EmitUniqueEvent:
                            TestScenarioEventSource.Log.UniqueEvent(TestAppScenarios.TraceEvents.UniqueEventMessage);
                            break;
                        case TestAppScenarios.TraceEvents.Commands.ShutdownScenario:
                            stopGeneratingEvents.Set();
                            eventEmitterTask.Wait(token);
                            return 0;
                    }
                }
            }, token);
        }
    }
}
