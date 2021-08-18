// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading;

namespace Microsoft.Diagnostics.Monitoring.ExecuteApp
{
    internal class Program
    {
        public static int Main(string[] args)
        {
            //File.WriteAllText("C:\\Users\\kkeirstead\\FileOutputs\\Test.txt", args[0]); // Testing purposes only

            string testType = args[0];
            const int DelayMs = 3000; // Should be greater than the token delay in the test.

            switch (testType)
            {
                case "ZeroExitCode":
                    return 0; ;

                case "NonzeroExitCode":
                    return -1;

                case "TokenCancellation":
                    Thread.Sleep(DelayMs);
                    return 0;

                case "TextFileOutput":
                    //File.WriteAllText("C:\\Users\\kkeirstead\\FileOutputs\\Test2.txt", args[1]); // Testing purposes only
                    //File.WriteAllText("C:\\Users\\kkeirstead\\FileOutputs\\Test3.txt", args[2]); // Testing purposes only

                    string textFilePath = args[1];
                    string textFileMessage = args[2];

                    File.WriteAllText(textFilePath, textFileMessage);
                    return 0;

                default:
                    return -100; // Arbitrary nonzero exit code
            }
        }
    }
}
