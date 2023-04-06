// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios;
using System.CommandLine;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.UnitTestApp
{
    internal sealed class Program
    {
        public static Task<int> Main(string[] args)
        {
            CliRootCommand root = new()
            {
                AsyncWaitScenario.Command(),
                ExecuteScenario.Command(),
                LoggerScenario.Command(),
                SpinWaitScenario.Command(),
                EnvironmentVariablesScenario.Command(),
                StacksScenario.Command()
            };

            return root.Parse(args).InvokeAsync();
        }
    }
}
