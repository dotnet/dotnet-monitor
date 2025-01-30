// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios;
using Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios.FunctionProbes;
using System.CommandLine;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.UnitTestApp
{
    internal sealed class Program
    {
        public static Task<int> Main(string[] args)
        {
            RootCommand root = new()
            {
                MetricsScenario.Command(),
                AspNetScenario.Command(),
                AsyncWaitScenario.Command(),
                ExceptionsScenario.Command(),
                ExecuteScenario.Command(),
                FunctionProbesScenario.Command(),
                LoggerScenario.Command(),
                ParameterCapturingScenario.Command(),
                SpinWaitScenario.Command(),
                EnvironmentVariablesScenario.Command(),
                StacksScenario.Command(),
                TraceEventsScenario.Command()
            };

            return root.Parse(args).InvokeAsync();
        }
    }
}
