// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    internal static class ActionTestsHelper
    {
        public const string ExpectedEgressProvider = "TmpEgressProvider";
        public const string ZeroExitCode = "ZeroExitCode";
        public const string NonzeroExitCode = "NonzeroExitCode";
        public const string Sleep = "Sleep";
        public const string TextFileOutput = "TextFileOutput";

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
    }
}
