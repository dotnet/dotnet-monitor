// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if !UNITTEST
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
#endif
using System.Collections.Generic;

#if UNITTEST
namespace Microsoft.Diagnostics.Monitoring.TestCommon.Options
#else
namespace Microsoft.Diagnostics.Tools.Monitor
#endif
{
    internal sealed class RootOptions
    {
        public ApiAuthenticationOptions ApiAuthentication { get; set; }

        public IDictionary<string, CollectionRuleOptions> CollectionRules { get; }
            = new Dictionary<string, CollectionRuleOptions>(0);

        public CorsConfiguration CorsConfiguration { get; set; }

        public DiagnosticPortOptions DiagnosticPort { get; set; }

        public EgressOptions Egress { get; set; }

        public MetricsOptions Metrics { get; set; }

        public StorageOptions Storage { get; set; }

        public ProcessFilterOptions DefaultProcess { get; set; }
    }
}
