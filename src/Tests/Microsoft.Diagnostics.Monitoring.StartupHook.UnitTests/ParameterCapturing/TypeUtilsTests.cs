// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.UnitTests.ParameterCapturing
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public class TypeUtilsTests
    {
        [Theory]
        [InlineData("ClassInNamespace", "ClassInNamespace.MyClass", true)]
        [InlineData("NestedType", "NestedType+MyNestedType", true)]
        [InlineData("SameAsNamespace", "SameAsNamespace", true)]
        [InlineData("DifferentCasing", "differentcasing", false)]
        [InlineData("CustomNamespace.Microsoft", "Microsoft", false)]
        [InlineData("SubString2", "SubString", false)]
        [InlineData("SubString", "SubString2", false)]
        public void IsSubType(string parentType, string typeName, bool isSubType)
        {
            Assert.Equal(isSubType, TypeUtils.IsSubType(parentType, typeName));
        }
    }
}
