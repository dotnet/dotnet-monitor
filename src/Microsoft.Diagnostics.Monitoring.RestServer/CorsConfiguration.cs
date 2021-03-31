// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

#if UNITTEST
namespace Microsoft.Diagnostics.Monitoring.UnitTests.Options
#else
namespace Microsoft.Diagnostics.Monitoring.RestServer
#endif
{
    public class CorsConfiguration
    {
        [Description("List of allowed cors origins, separated by ;")]
        [Required]
        public string AllowedOrigins { get; set; }

        public string[] GetOrigins() => AllowedOrigins?.Split(';');
    }
}
