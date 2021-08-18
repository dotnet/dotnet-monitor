// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading;

namespace Microsoft.Diagnostics.Monitoring.ExecuteActionApp
{
    internal class Program
    {
        public static int Main(string[] args)
        {
            string testType = args[0];
            const int DelayMs = 3000; // Must be greater than TokenTimeoutMs in ExecuteActionTests.

            switch (testType)
            {
                case "ZeroExitCode":
                    return 0;

                case "NonzeroExitCode":
                    return -1;

                case "TokenCancellation":
                    Thread.Sleep(DelayMs);
                    return 0;

                case "TextFileOutput":
                    File.WriteAllText(args[1], args[2]);
                    return 0;

                default:
                    return -100; // Arbitrary nonzero exit code
            }
        }
    }
}
