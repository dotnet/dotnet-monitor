// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.DataAnnotations;

#if UNITTEST
namespace Microsoft.Diagnostics.Monitoring.UnitTests.Options
#else
namespace Microsoft.Diagnostics.Tools.Monitor
#endif
{
    internal sealed class ApiAuthenticationOptions
    {
        [Display(
            ResourceType = typeof(Microsoft.Diagnostics.Tools.Monitor.SharedStrings), 
            Name = nameof(Microsoft.Diagnostics.Tools.Monitor.SharedStrings.DisplayAttributeDescription_ApiAuthenticationOptions_ApiKeyHash))]
        [RegularExpression("[0-9a-fA-F]+")]
        [MinLength(64)]
        [Required]
        public string ApiKeyHash { get; set; }

        [Display(
            ResourceType = typeof(Microsoft.Diagnostics.Tools.Monitor.SharedStrings),
            Name = nameof(Microsoft.Diagnostics.Tools.Monitor.SharedStrings.DisplayAttributeDescription_ApiAuthenticationOptions_ApiKeyHashType))]
        [Required]
        public string ApiKeyHashType { get; set; }
    }
}
