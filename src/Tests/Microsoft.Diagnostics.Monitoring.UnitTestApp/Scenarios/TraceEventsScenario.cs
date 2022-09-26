// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios
{
    /// <summary>
    /// Continously emits trace events and a unique one on request.
    /// Only stops once an exit request is received.
    /// </summary>
    internal class TraceEventsScenario
    {
        [EventSource(Name = "TestScenario")]
        class TestScenarioEventSource : EventSource
        {
            public static TestScenarioEventSource Log { get; } = new TestScenarioEventSource();

            [Event(1)]
            public void RandomNumberGenerated(int number) => WriteEvent(1, number);

            [Event(2, Opcode = EventOpcode.Reply)]
            public void UniqueEvent() => WriteEvent(2);
        }

        public static Command Command()
        {
            Command command = new(TestAppScenarios.TraceEvents.Name);
            command.SetHandler(ExecuteAsync);
            return command;
        }

        public static async Task ExecuteAsync(InvocationContext context)
        {
            string[] acceptableCommands = new string[]
            {
                TestAppScenarios.TraceEvents.Commands.EmitUniqueEvent,
                TestAppScenarios.TraceEvents.Commands.ShutdownScenario
            };

            context.ExitCode = await ScenarioHelpers.RunScenarioAsync(async logger =>
            {
                TaskCompletionSource<object> stopGeneratingEvents = new();
                Task eventEmitterTask = Task.Run(() =>
                {
                    Random random = new();
                    while (!stopGeneratingEvents.Task.IsCompleted)
                    {
                        TestScenarioEventSource.Log.RandomNumberGenerated(random.Next());
                        Task.Delay(100, context.GetCancellationToken());
                    }
                });

                while (true)
                {
                    switch (await ScenarioHelpers.WaitForCommandAsync(acceptableCommands, logger))
                    {
                        case TestAppScenarios.TraceEvents.Commands.EmitUniqueEvent:
                            TestScenarioEventSource.Log.UniqueEvent();
                            break;
                        case TestAppScenarios.TraceEvents.Commands.ShutdownScenario:
                            stopGeneratingEvents.TrySetResult(null);
                            eventEmitterTask.Wait(context.GetCancellationToken());
                            return 0;
                    }
                }
            }, context.GetCancellationToken());
        }
    }
}
