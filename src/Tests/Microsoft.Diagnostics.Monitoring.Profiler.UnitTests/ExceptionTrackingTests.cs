// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.Diagnostics.Monitoring.Profiler.UnitTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public sealed class ExceptionTrackingTests
    {
        private const string IgnoreOutputPrefix = "[ignore]";
        private const string ProfilerOutputPrefix = "[profiler]";

        private const string BaselinesFolderName = "Baselines";
        private const string OutputsFolderName = "Outputs";

        private readonly ITestOutputHelper _outputHelper;

        public ExceptionTrackingTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Theory(Skip = "Exception tracking via profiler is currently disabled")]
        [MemberData(nameof(ProfilerHelper.GetNotifyOnlyArchitectureProfilerPath), MemberType = typeof(ProfilerHelper))]
        public Task ExceptionThrowCatch(Architecture architecture, string profilerPath, ProfilerVariant variant)
        {
            Assert.Equal(ProfilerVariant.NotifyOnly, variant);
            return RunAndCompare(nameof(ExceptionThrowCatch), architecture, profilerPath);
        }

        [Theory(Skip = "Exception tracking via profiler is currently disabled")]
        [MemberData(nameof(ProfilerHelper.GetNotifyOnlyArchitectureProfilerPath), MemberType = typeof(ProfilerHelper))]
        public Task ExceptionThrowCrash(Architecture architecture, string profilerPath, ProfilerVariant variant)
        {
            Assert.Equal(ProfilerVariant.NotifyOnly, variant);
            return RunAndCompare(nameof(ExceptionThrowCrash), architecture, profilerPath);
        }

        private async Task RunAndCompare(string scenarioName, Architecture architecture, string profilerPath)
        {
            ITestOutputHelper appOutputHelper = new PrefixedOutputHelper(_outputHelper, FormattableString.Invariant($"[App] "));

            using DotNetRunner runner = new();
            runner.Architecture = architecture;
            await using LoggingRunnerAdapter adapter = new(appOutputHelper, runner);

            runner.EntrypointAssemblyPath = AssemblyHelper.GetAssemblyArtifactBinPath(
                Assembly.GetExecutingAssembly(),
                "Microsoft.Diagnostics.Monitoring.Profiler.UnitTestApp");
            runner.Arguments = scenarioName;

            // Environment variables necessary for running the profiler + enable all logging to stderr
            adapter.Environment.Add(ProfilerHelper.ClrEnvVarEnableNotificationProfilers, ProfilerHelper.ClrEnvVarEnabledValue);
            adapter.Environment.Add(ProfilerHelper.ClrEnvVarEnableProfiling, ProfilerHelper.ClrEnvVarEnabledValue);
            adapter.Environment.Add(ProfilerHelper.ClrEnvVarProfiler, ProfilerIdentifiers.NotifyOnlyProfiler.Clsid.StringWithBraces);
            adapter.Environment.Add(ProfilerHelper.ClrEnvVarProfilerPath, profilerPath);
            adapter.Environment.Add(ProfilerIdentifiers.EnvironmentVariables.RuntimeInstanceId, Guid.NewGuid().ToString("D"));
            adapter.Environment.Add(ProfilerIdentifiers.EnvironmentVariables.StdErrLogger_Level, LogLevel.Trace.ToString("G"));

            List<string> outputLines = new();

            Action<string> receivedStdErrLine = (line) =>
            {
                if (!string.IsNullOrEmpty(line))
                {
                    // Only care to capture lines that start with "[profiler]". Other lines
                    // will have "[ignore]" prepended to allow for capture, but ignored during
                    // analysis.
                    if (line.StartsWith(ProfilerOutputPrefix, StringComparison.Ordinal))
                    {
                        outputLines.Add(line);
                    }
                    else
                    {
                        outputLines.Add($"{IgnoreOutputPrefix}{line}");
                    }
                }
            };

            adapter.ReceivedStandardErrorLine += receivedStdErrLine;

            using CancellationTokenSource startTokenSource = new(CommonTestTimeouts.StartProcess);
            await adapter.StartAsync(startTokenSource.Token);

            using CancellationTokenSource waitForExitSource = new(CommonTestTimeouts.WaitForExit);
            await adapter.ReadToEnd(waitForExitSource.Token);

            adapter.ReceivedStandardErrorLine -= receivedStdErrLine;

            string fileName = $"{scenarioName}.txt";

            Directory.CreateDirectory(OutputsFolderName);
            File.WriteAllLines(Path.Combine(OutputsFolderName, fileName), outputLines);

            string[] baselineLines = File.ReadAllLines(Path.Combine(BaselinesFolderName, fileName));

            try
            {
                // The current index in the list of lines
                int baselineIndex = 0;
                int outputIndex = 0;

                // The count of non-ignored lines
                int baselineCount = 0;
                int outputCount = 0;

                // The current line value
                string baselineLine = null;
                string outputLine = null;

                // Try to read a line from each list and compare them
                while (TryReadNextLine(baselineLines, out baselineLine, ref baselineIndex, ref baselineCount) &&
                       TryReadNextLine(outputLines, out outputLine, ref outputIndex, ref outputCount))
                {
                    try
                    {
                        Assert.Equal(baselineLine, outputLine);
                    }
                    catch (XunitException)
                    {
                        // baselineIndex is already incremented, thus in terms of line numbers, it is already correct
                        _outputHelper.WriteLine($"Difference on line {baselineIndex} of baseline.");

                        throw;
                    }
                }

                // Read the remaining lines from each in order to get the total line count
                while (TryReadNextLine(baselineLines, out baselineLine, ref baselineIndex, ref baselineCount)) { }
                while (TryReadNextLine(outputLines, out outputLine, ref outputIndex, ref outputCount)) { }

                // The total un-ignored line count should be equal
                Assert.Equal(baselineCount, outputCount);
            }
            catch (XunitException)
            {
                _outputHelper.WriteLine("=== Begin Output ===");
                for (int index = 0; index < outputLines.Count; index++)
                {
                    _outputHelper.WriteLine(outputLines[index]);
                }
                _outputHelper.WriteLine("=== End Output =====");

                throw;
            }
        }

        private static bool TryReadNextLine(IReadOnlyList<string> lines, out string line, ref int index, ref int lineCount)
        {
            line = null;
            while (index < lines.Count)
            {
                string candidate = lines[index];
                index++;

                // Anything that doesn't start with "[ignore]" is considered a valid line for analysis
                if (string.IsNullOrEmpty(candidate) || !candidate.StartsWith(IgnoreOutputPrefix))
                {
                    lineCount++;

                    line = candidate;
                    return true;
                }
            }
            return false;
        }
    }
}
