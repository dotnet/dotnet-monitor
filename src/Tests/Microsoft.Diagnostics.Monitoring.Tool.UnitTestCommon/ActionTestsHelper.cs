// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    internal static class ActionTestsHelper
    {
        private static TargetFrameworkMoniker[] tfmsToTest =
        [
            TargetFrameworkMoniker.Net60,
            TargetFrameworkMoniker.Net70,
            TargetFrameworkMoniker.Net80
        ];

        private static TargetFrameworkMoniker[] tfms6PlusToTest =
        [
            TargetFrameworkMoniker.Net60,
            TargetFrameworkMoniker.Net70,
            TargetFrameworkMoniker.Net80
        ];

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
                yield return new object[] { tfm, TraceProfile.GcCollect };
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
                if (!DistroInformation.IsAlpineLinux
                    || tfm == TargetFrameworkMoniker.Net60
                    || tfm == TargetFrameworkMoniker.Net70
                    || tfm == TargetFrameworkMoniker.Net80)
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

        public static IEnumerable<object[]> GetTfmArchitectureProfilerPath()
        {
            // There isn't a good way to check which architecture to use when running unit tests.
            // Each build job builds one specific architecture, but from a test perspective,
            // it cannot tell which one was built. Gather all of the profilers for every architecture
            // so long as they exist.
            List<object[]> arguments = new();
            AddTestCases(arguments, Architecture.X64);
            AddTestCases(arguments, Architecture.X86);
            AddTestCases(arguments, Architecture.Arm64);
            return arguments;

            static void AddTestCases(List<object[]> arguments, Architecture architecture)
            {
                string profilerPath = ProfilerHelper.GetPath(architecture);
                if (File.Exists(profilerPath))
                {
                    foreach (TargetFrameworkMoniker tfm in ActionTestsHelper.tfms6PlusToTest)
                    {
                        arguments.Add(new object[] { tfm, architecture, profilerPath });
                    }
                }
            }
        }

        internal static string ValidateEgressPath(CollectionRuleActionResult result, string expectedArtifactName = null)
        {
            Assert.NotNull(result.OutputValues);
            Assert.True(result.OutputValues.TryGetValue(CollectionRuleActionConstants.EgressPathOutputValueName, out string egressPath));
            Assert.True(File.Exists(egressPath));

            if (expectedArtifactName != null)
                Assert.Equal(expectedArtifactName, Path.GetFileName(egressPath));

            return egressPath;
        }

        internal static T GetActionOptions<T>(IHost host, string ruleName, int actionIndex = 0)
        {
            IOptionsMonitor<CollectionRuleOptions> ruleOptionsMonitor = host.Services.GetService<IOptionsMonitor<CollectionRuleOptions>>();
            T options = (T)ruleOptionsMonitor.Get(ruleName).Actions[actionIndex].Settings;

            return options;
        }

        internal static async Task<CollectionRuleActionResult> ExecuteAndDisposeAsync(ICollectionRuleAction action, TimeSpan timeout)
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
