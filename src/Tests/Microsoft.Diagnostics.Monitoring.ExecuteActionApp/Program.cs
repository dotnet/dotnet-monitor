// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.ExecuteActionApp
{
    internal class Program
    {
        public static int Main(string[] args)
        {
            string testType = args[1];
            const int DelayMs = 3000; // Should be greater than the token delay in the test.

            switch (testType)
            {
                case "ZeroExitCode":
                    return 0; ;

                case "NonzeroExitCode":
                    return -1;

                case "TokenCancellation":
                    Task.Delay(DelayMs);
                    return 0;

                default:
                    return -100;
            }
        }
    }
}
