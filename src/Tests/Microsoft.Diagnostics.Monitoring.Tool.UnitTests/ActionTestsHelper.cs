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

        public static IEnumerable<object[]> GetTfms()
        {
            foreach (TargetFrameworkMoniker tfm in tfmsToTest)
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
                yield return new object[] { tfm, DumpType.WithHeap };
                yield return new object[] { tfm, DumpType.Triage };
                yield return new object[] { tfm, DumpType.Mini };
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

        internal static string ValidateEgressPath(CollectionRuleActionResult result)
        {
            Assert.NotNull(result.OutputValues);
            Assert.True(result.OutputValues.TryGetValue(CollectionRuleActionConstants.EgressPathOutputValueName, out string egressPath));
            Assert.True(File.Exists(egressPath));

            return egressPath;
        }

        internal static T GetOptions<T>(string ruleName, IHost host)
        {
            IOptionsMonitor<CollectionRuleOptions> ruleOptionsMonitor = host.Services.GetService<IOptionsMonitor<CollectionRuleOptions>>();
            T options = (T)ruleOptionsMonitor.Get(ruleName).Actions[0].Settings;

            return options;
        }

        internal async static Task<CollectionRuleActionResult> PerformAction(ICollectionRuleAction action, TimeSpan timeout)
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
