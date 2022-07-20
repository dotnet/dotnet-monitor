// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    /// <summary>
    /// This is a version of <see cref="JwtBearerHandler" /> that will refresh the 
    /// stored JwtBearerOptions options when the settings change.
    /// </summary>
    internal sealed class LiveJwtBearerHandler : JwtBearerHandler
    {
        public LiveJwtBearerHandler(IOptionsMonitor<JwtBearerOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
            options.OnChange((JwtBearerOptions opts, string name) =>
            {
                // This is required to get AuthenticationHandler<JwtBearerOptions> to reload options.
                // Once InitializeAsync is called, the value of the JwtBearerOptions is stored in Options and updates are never taken.
                base.InitializeAsync(Scheme, Context);
            });
        }
    }
}
