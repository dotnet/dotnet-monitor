// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    public class TargetFrameworkMonikerComparerTests
    {
        /// <summary>
        /// Tests that comparer reports results correctly for each TFM combination.
        /// </summary>
        [Fact]
        public void ConcreteValuesTest()
        {
            IComparer<TargetFrameworkMoniker> comparer = TargetFrameworkMonikerComparer.Default;
            Assert.Equal(0, comparer.Compare(TargetFrameworkMoniker.NetCoreApp31, TargetFrameworkMoniker.NetCoreApp31));
            Assert.True(0 > comparer.Compare(TargetFrameworkMoniker.NetCoreApp31, TargetFrameworkMoniker.Net50));
            Assert.True(0 > comparer.Compare(TargetFrameworkMoniker.NetCoreApp31, TargetFrameworkMoniker.Net60));
            Assert.True(0 < comparer.Compare(TargetFrameworkMoniker.Net50, TargetFrameworkMoniker.NetCoreApp31));
            Assert.Equal(0, comparer.Compare(TargetFrameworkMoniker.Net50, TargetFrameworkMoniker.Net50));
            Assert.True(0 > comparer.Compare(TargetFrameworkMoniker.Net50, TargetFrameworkMoniker.Net60));
            Assert.True(0 < comparer.Compare(TargetFrameworkMoniker.Net60, TargetFrameworkMoniker.NetCoreApp31));
            Assert.True(0 < comparer.Compare(TargetFrameworkMoniker.Net60, TargetFrameworkMoniker.Net50));
            Assert.Equal(0, comparer.Compare(TargetFrameworkMoniker.Net60, TargetFrameworkMoniker.Net60));
        }
    }
}
