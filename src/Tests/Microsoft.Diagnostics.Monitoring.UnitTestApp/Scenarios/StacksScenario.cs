﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios
{
    internal class StacksScenario
    {
        [DllImport(ProfilerIdentifiers.LibraryRootFileName, CallingConvention = CallingConvention.StdCall, PreserveSig = true)]
        public static extern int TestHook([MarshalAs(UnmanagedType.FunctionPtr)] Action callback);

        static StacksScenario()
        {
            NativeLibrary.SetDllImportResolver(typeof(StacksScenario).Assembly, ResolveDllImport);
        }

        public static IntPtr ResolveDllImport(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            //DllImport for Windows automatically loads in-memory modules (such as the profiler). This is not the case for Linux/MacOS.
            //If we fail resolving the DllImport, we have to load the profiler ourselves.

            string profilerName = ProfilerHelper.GetPath(RuntimeInformation.ProcessArchitecture);
            if (NativeLibrary.TryLoad(profilerName, out IntPtr handle))
            {
                return handle;
            }

            return IntPtr.Zero;
        }

        public static Command Command()
        {
            Command command = new(TestAppScenarios.Stacks.Name);

            command.SetHandler(ExecuteAsync);
            return command;
        }

        public static async Task ExecuteAsync(InvocationContext context)
        {
            using StacksWorker worker = new StacksWorker();

            //Background thread will create an expected callstack and pause.
            Thread thread = new Thread(Entrypoint);
            thread.Start(worker);

            context.ExitCode = await ScenarioHelpers.RunScenarioAsync(async logger =>
            {
                await ScenarioHelpers.WaitForCommandAsync(TestAppScenarios.Stacks.Commands.Continue, logger);

                //Allow the background thread to resume work.
                worker.Signal();

                return 0;
            }, context.GetCancellationToken());
        }

        public static void Entrypoint(object worker)
        {
            var stacksWorker = (StacksWorker)worker;
            stacksWorker.Work();
        }
    }
}
