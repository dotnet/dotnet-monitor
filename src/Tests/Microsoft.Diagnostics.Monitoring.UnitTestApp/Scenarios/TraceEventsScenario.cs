﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics.Tracing;
using System.Threading;
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
            public void UniqueEvent(string message) => WriteEvent(2, message);
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
                using ManualResetEventSlim stopGeneratingEvents = new(initialState: false);

                Task eventEmitterTask = Task.Run(async () =>
                {
                    Random random = new();
                    while (!stopGeneratingEvents.IsSet)
                    {
                        TestScenarioEventSource.Log.RandomNumberGenerated(random.Next());
                        await Task.Delay(TimeSpan.FromMilliseconds(100), context.GetCancellationToken());
                    }
                }, context.GetCancellationToken());

                while (true)
                {
                    switch (await ScenarioHelpers.WaitForCommandAsync(acceptableCommands, logger))
                    {
                        case TestAppScenarios.TraceEvents.Commands.EmitUniqueEvent:
                            TestScenarioEventSource.Log.UniqueEvent(TestAppScenarios.TraceEvents.UniqueEventMessage);
                            break;
                        case TestAppScenarios.TraceEvents.Commands.ShutdownScenario:
                            stopGeneratingEvents.Set();
                            eventEmitterTask.Wait(context.GetCancellationToken());
                            return 0;
                    }
                }
            }, context.GetCancellationToken());
        }
    }
}
