// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Diagnostics.Monitoring.EgressExtensibilityApp
{
    /// <summary>
    /// Egress provider options for Azure blob storage.
    /// </summary>
    internal sealed partial class TestEgressProviderOptions
    {
        public bool ShouldSucceed { get; set; }
    }
}
