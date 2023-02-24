// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.UnitTestApp
{
    internal sealed class Program
    {
        public static Task<int> Main(string[] args)
        {
            return new CommandLineBuilder(new RootCommand()
            {
                MetricsScenario.Command(),
                AspNetScenario.Command(),
                AsyncWaitScenario.Command(),
                ExceptionsScenario.Command(),
                ExecuteScenario.Command(),
                LoggerScenario.Command(),
                SpinWaitScenario.Command(),
                EnvironmentVariablesScenario.Command(),
                StacksScenario.Command(),
                TraceEventsScenario.Command()
            })
            .UseDefaults()
            .Build()
            .InvokeAsync(args);
        }
    }
}
