﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.ExecuteActionApp
{
    internal class Program
    {
        private static readonly ILogger _logger;

        public static int Main(string[] args)
        {
            string testType = args[0];

            string[] testArgs = args.Skip(1).ToArray();

            switch (testType)
            {
                case "ZeroExitCode":
                    Assert.Equal(0, testArgs.Length);
                    return 0;

                case "NonzeroExitCode":
                    Assert.Equal(0, testArgs.Length);
                    return 1;

                case "Sleep":
                    Assert.Equal(1, testArgs.Length);
                    string delayArg = testArgs[0];
                    int delay = int.Parse(delayArg);
                    Thread.Sleep(delay);
                    return 0;

                case "TextFileOutput":
                    Assert.Equal(2, testArgs.Length);
                    string pathArg = testArgs[0];
                    string contentsArg = testArgs[1];
                    File.WriteAllText(pathArg, contentsArg);
                    return 0;

                case "Logging":
                    _logger.LogInformation("This is a test 1.");
                    Thread.Sleep(2000);
                    _logger.LogInformation("This is a test 2.");
                    Thread.Sleep(2000);
                    _logger.LogInformation("This is a test 3.");
                    Thread.Sleep(10000);

                    return 0;

                default:
                    throw new ArgumentException($"Unknown test type {testType}.");
            }
        }
    }
}
