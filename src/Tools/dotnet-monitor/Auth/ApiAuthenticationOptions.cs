// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

#if UNITTEST
namespace Microsoft.Diagnostics.Monitoring.UnitTests.Options
#else
namespace Microsoft.Diagnostics.Tools.Monitor
#endif
{
    internal sealed class ApiAuthenticationOptions
    {
        [Description("API Key in Hashed form. Each byte should be two hex based digits.")]
        [RegularExpression("[0-9a-fA-F]+")]
        [MinLength(64)]
        [Required]
        public string ApiKeyHash { get; set; }

        [Description("Type of Hash Algorithm used to computed ApiKeyHash. Typically SHA256.  SHA1 and MD5 are not allowed.")]
        [Required]
        public string ApiKeyHashType { get; set; }
    }
}
