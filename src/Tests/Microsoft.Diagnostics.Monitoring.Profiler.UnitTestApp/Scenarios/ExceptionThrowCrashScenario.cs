// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace Microsoft.Diagnostics.Monitoring.Profiler.UnitTestApp.Scenarios
{
    /// <summary>
    /// Async waits until it receives the Continue command.
    /// </summary>
    internal static class ExceptionThrowCrashScenario
    {
        public static Command Command()
        {
            Command command = new("ExceptionThrowCrash");
            command.SetHandler(Execute);
            return command;
        }

        public static int Execute(InvocationContext context)
        {
            throw new InvalidOperationException();
        }
    }
}
