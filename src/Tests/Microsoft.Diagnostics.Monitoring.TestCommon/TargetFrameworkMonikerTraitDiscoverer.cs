// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    internal class TargetFrameworkMonikerTraitDiscoverer : ITraitDiscoverer
    {
        public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
        {
            TargetFrameworkMoniker tfm = traitAttribute.GetConstructorArguments().OfType<TargetFrameworkMoniker>().Single();
            yield return new KeyValuePair<string, string>("TargetFrameworkMoniker", tfm.ToString());
        }
    }
}
