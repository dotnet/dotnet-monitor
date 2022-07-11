// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.UnitTestApp
{
    internal class Program
    {
        public static Task<int> Main(string[] args)
        {
            return new CommandLineBuilder(new RootCommand()
            {
                AsyncWaitScenario.Command(),
                LoggerScenario.Command(),
                SpinWaitScenario.Command(),
                EnvironmentVariablesScenario.Command()
            })
            .UseDefaults()
            .Build()
            .InvokeAsync(args);
        }
    }
}
