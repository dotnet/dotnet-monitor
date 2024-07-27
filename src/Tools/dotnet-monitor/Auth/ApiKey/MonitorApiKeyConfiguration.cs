// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Tools.Monitor.Auth.ApiKey
{
    /// <summary>
    /// Represents internal version of <see cref="MonitorApiKeyOptions"/>.
    /// This object contains the validation state of the object and the decoded
    /// Json Web Key.
    /// </summary>
    internal class MonitorApiKeyConfiguration : AuthenticationSchemeOptions
    {
        public bool Configured { get; set; }
        public string? Subject { get; set; }
        public SecurityKey? PublicKey { get; set; }
        public IEnumerable<ValidationResult>? ValidationErrors { get; set; }
        public string? Issuer { get; set; }
    }
}
