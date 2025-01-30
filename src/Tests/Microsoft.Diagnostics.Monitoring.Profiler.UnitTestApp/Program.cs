// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Profiler.UnitTestApp.Scenarios;
using System.CommandLine;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.Profiler.UnitTestApp
{
    internal static class Program
    {
        public static Task<int> Main(string[] args)
        {
            RootCommand root = new()
            {
                ExceptionThrowCatchScenario.Command(),
                ExceptionThrowCrashScenario.Command()
            };

            return root.Parse(args).InvokeAsync();
        }
    }
}
