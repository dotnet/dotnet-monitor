// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.UnitTestApp
{
    internal class Program
    {
        public static async Task<int> Main(string[] args)
        {
            try
            {
                int result = await new CommandLineBuilder()
                    .AddCommand(AsyncWaitScenario.Command())
                    .AddCommand(LoggerScenario.Command())
                    .UseDefaults()
                    .Build()
                    .InvokeAsync(args);
                return result;
            }
            catch (System.Exception e)
            {
                System.Console.Error.WriteLine(e.ToString());
                System.Console.Error.Flush();
                return -55;
            }

        }
    }
}
