// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Diagnostics.Monitoring.UnitTests.Options
{
    internal class RootOptions
    {
        public ApiAuthenticationOptions ApiAuthentication { get; set; }

        public CorsConfiguration CorsConfiguration { get; set; }

        public DiagnosticPortOptions DiagnosticPort { get; set; }

        public EgressOptions Egress { get; set; }

        public MetricsOptions Metrics { get; set; }

        public StorageOptions Storage { get; set; }

        public ProcessFilterOptions DefaultProcess { get; set; }
    }
}
