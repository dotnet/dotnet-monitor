// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    public static class CommonMemberDataParameters
    {
        private static readonly Lazy<IEnumerable<Tuple<TargetFrameworkMoniker, DiagnosticPortConnectionMode>>> s_allTfmsAndConnectionModesLazy
            = new Lazy<IEnumerable<Tuple<TargetFrameworkMoniker, DiagnosticPortConnectionMode>>>(GetAllTfmsAndConnectionModes);

        public static readonly TargetFrameworkMoniker[] AllTfms = new[] { TargetFrameworkMoniker.NetCoreApp31, TargetFrameworkMoniker.Net50, TargetFrameworkMoniker.Net60 };

        public static IEnumerable<Tuple<TargetFrameworkMoniker, DiagnosticPortConnectionMode>> AllTfmsAndConnectionModes => s_allTfmsAndConnectionModesLazy.Value;

        /// <summary>
        /// Get all testable TFMs.
        /// </summary>
        public static IEnumerable<object[]> GetTfmParameters()
        {
            foreach (TargetFrameworkMoniker tfm in AllTfms)
            {
                yield return new object[] { tfm };
            }
        }

        /// <summary>
        /// Return a list of arrays whose elements are the TFM of the target application
        /// and the connection mode of the test host (unit test) or dotnet-monitor (functional test).
        /// </summary>
        public static IEnumerable<object[]> GetTfmAndConnectionModeParameters()
        {
            foreach (Tuple<TargetFrameworkMoniker, DiagnosticPortConnectionMode> tuple in AllTfmsAndConnectionModes)
            {
                yield return new object[] { tuple.Item1, tuple.Item2 };
            }
        }

        private static IEnumerable<Tuple<TargetFrameworkMoniker, DiagnosticPortConnectionMode>> GetAllTfmsAndConnectionModes()
        {
            foreach (TargetFrameworkMoniker tfm in AllTfms)
            {
                yield return Tuple.Create<TargetFrameworkMoniker, DiagnosticPortConnectionMode>(tfm, DiagnosticPortConnectionMode.Connect);
                if (tfm.IsSameOrHigherThan(TargetFrameworkMoniker.Net50))
                {
                    yield return Tuple.Create<TargetFrameworkMoniker, DiagnosticPortConnectionMode>(tfm, DiagnosticPortConnectionMode.Listen);
                }
            }
        }
    }
}
