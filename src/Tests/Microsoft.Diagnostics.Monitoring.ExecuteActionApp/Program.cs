// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.ExecuteActionApp
{
    internal class Program
    {
        public static int Main(string[] args)
        {
            string testType = args[0];

            string[] additionalArgs = args.Skip(1).ToArray();

            switch (testType)
            {
                case "ZeroExitCode":
                    Assert.Equal(1, args.Length);
                    return 0;

                case "NonzeroExitCode":
                    Assert.Equal(1, args.Length);
                    return -1;

                case "Sleep":
                    Assert.Equal(2, args.Length);
                    string delayArg = additionalArgs[0];
                    int delay = int.Parse(delayArg) + 1000; // Add a second delay to the token cancellation time
                    Thread.Sleep(delay);
                    return 0;

                case "TextFileOutput":
                    Assert.Equal(3, args.Length);
                    string pathArg = additionalArgs[0];
                    string contentsArg = additionalArgs[1];
                    File.WriteAllText(pathArg, contentsArg);
                    return 0;

                default:
                    throw new ArgumentException("Unknown provided test type.");
            }
        }
    }
}
