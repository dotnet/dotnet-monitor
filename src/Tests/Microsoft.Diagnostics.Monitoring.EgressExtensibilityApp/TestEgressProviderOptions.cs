// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.Diagnostics.Monitoring.EgressExtensibilityApp
{
    internal sealed partial class TestEgressProviderOptions
    {
        public bool ShouldSucceed { get; set; }
        public IDictionary<string, string> Metadata { get; set; }
            = new Dictionary<string, string>(0);
    }
}
