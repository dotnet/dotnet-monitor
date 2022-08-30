// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.CommandLine;

namespace Microsoft.Diagnostics.Monitoring.Profiler.UnitTestApp.Scenarios
{
    /// <summary>
    /// Async waits until it receives the Continue command.
    /// </summary>
    internal class ExceptionThrowCrashScenario
    {
        public static Command Command()
        {
            Command command = new("ExceptionThrowCrash");
            command.SetHandler(Execute);
            return command;
        }

        public static void Execute()
        {
            throw new InvalidOperationException();
        }
    }
}
