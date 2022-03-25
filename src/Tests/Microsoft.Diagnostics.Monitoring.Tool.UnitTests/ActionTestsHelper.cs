// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    internal static class ActionTestsHelper
    {
        public static TargetFrameworkMoniker[] tfmsToTest = new TargetFrameworkMoniker[] { TargetFrameworkMoniker.Net50, TargetFrameworkMoniker.Net60 };
        public static TargetFrameworkMoniker[] tfms6PlusToTest = new TargetFrameworkMoniker[] { TargetFrameworkMoniker.Net60 };

        public static IEnumerable<object[]> GetTfms()
        {
            foreach (TargetFrameworkMoniker tfm in tfmsToTest)
            {
                yield return new object[] { tfm };
            }
        }

        public static IEnumerable<object[]> Get6PlusTfms()
        {
            foreach (TargetFrameworkMoniker tfm in tfms6PlusToTest)
            {
                yield return new object[] { tfm };
            }
        }

        public static IEnumerable<object[]> GetTfmsAndTraceProfiles()
        {
            foreach (TargetFrameworkMoniker tfm in tfmsToTest)
            {
                yield return new object[] { tfm, TraceProfile.Logs };
                yield return new object[] { tfm, TraceProfile.Metrics };
                yield return new object[] { tfm, TraceProfile.Http };
                yield return new object[] { tfm, TraceProfile.Cpu };
            }
        }

        public static IEnumerable<object[]> GetTfmsAndDumpTypes()
        {
            foreach (TargetFrameworkMoniker tfm in tfmsToTest)
            {
                yield return new object[] { tfm, DumpType.Full };
                // Capturing non-full dumps via diagnostic command works inconsistently
                // on Alpine for .NET 5 and lower (the dump command will return successfully, but)
                // the dump file will not exist). Only test other dump types on .NET 6+
                if (!DistroInformation.IsAlpineLinux || tfm == TargetFrameworkMoniker.Net60)
                {
                    yield return new object[] { tfm, DumpType.WithHeap };
                    yield return new object[] { tfm, DumpType.Triage };
                    yield return new object[] { tfm, DumpType.Mini };
                }
            }
        }

        public static IEnumerable<object[]> GetTfmsAndLogFormat()
        {
            foreach (TargetFrameworkMoniker tfm in tfmsToTest)
            {
                yield return new object[] { tfm, LogFormat.NewlineDelimitedJson };
                yield return new object[] { tfm, LogFormat.JsonSequence };
            }
        }

        public static IEnumerable<object[]> GetTfmsAndProfilerPath()
        {
            // There isn't a good way to check if the test should be using the x86 or x64 profiler
            // when running unit tests. Each build job builds one specific architecture but from
            // a test perspective, it cannot tell which one was built. Additionally, only the x64
            // runtimes are installed at this time. Most test runs occur on x64, so try that one first.
            string profilerPath = NativeLibraryHelper.GetMonitorProfilerPath("x64");
            if (File.Exists(profilerPath))
            {
                foreach (TargetFrameworkMoniker tfm in ActionTestsHelper.tfms6PlusToTest)
                {
                    yield return new object[] { tfm, profilerPath };
                }
            }
            else
            {
                // If the x64 library could not be found, likely built the x86 library (it shouldn't
                // be arm64 since tests are not run on arm64). Check that x86 was built and pass the
                // test since the actual test cannot be run without the x86 runtimes.
                Assert.True(File.Exists(NativeLibraryHelper.GetMonitorProfilerPath("x86")));
            }
        }

        internal static string ValidateEgressPath(CollectionRuleActionResult result)
        {
            Assert.NotNull(result.OutputValues);
            Assert.True(result.OutputValues.TryGetValue(CollectionRuleActionConstants.EgressPathOutputValueName, out string egressPath));
            Assert.True(File.Exists(egressPath));

            return egressPath;
        }

        internal static T GetActionOptions<T>(IHost host, string ruleName, int actionIndex = 0)
        {
            IOptionsMonitor<CollectionRuleOptions> ruleOptionsMonitor = host.Services.GetService<IOptionsMonitor<CollectionRuleOptions>>();
            T options = (T)ruleOptionsMonitor.Get(ruleName).Actions[actionIndex].Settings;

            return options;
        }

        internal async static Task<CollectionRuleActionResult> ExecuteAndDisposeAsync(ICollectionRuleAction action, TimeSpan timeout)
        {
            using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(timeout);

            CollectionRuleActionResult result;
            try
            {
                await action.StartAsync(cancellationTokenSource.Token);

                result = await action.WaitForCompletionAsync(cancellationTokenSource.Token);
            }
            finally
            {
                await DisposableHelper.DisposeAsync(action);
            }

            return result;
        }
    }
}
