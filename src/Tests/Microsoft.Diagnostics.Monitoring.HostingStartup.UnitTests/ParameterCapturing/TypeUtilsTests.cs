// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.UnitTests.ParameterCapturing
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public class TypeUtilsTests
    {
        [Theory]
        [InlineData("Microsoft", "Microsoft.Diagnostics", true)]
        [InlineData("Microsoft", "Microsoft+Diagnostics", true)]
        [InlineData("Microsoft", "Microsoft", true)]
        [InlineData("Microsoft", "microsoft", false)]
        [InlineData("CustomNamespace.Microsoft", "Microsoft", false)]
        [InlineData("Microsoft2", "Microsoft", false)]
        public void DoesBelongToNamespace(string namespaceName, string typeName, bool isPartOf)
        {
            Assert.Equal(isPartOf, TypeUtils.DoesBelongToNamespace(namespaceName, typeName));
        }
    }
}
