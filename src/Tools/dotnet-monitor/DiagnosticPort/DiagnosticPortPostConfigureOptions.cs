// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class DiagnosticPortPostConfigureOptions :
        IPostConfigureOptions<DiagnosticPortOptions>
    {
        private readonly IConfiguration _configuration;

        public DiagnosticPortPostConfigureOptions(
            IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void PostConfigure(string name, DiagnosticPortOptions options)
        {
            IConfigurationSection diagPortSection = _configuration.GetSection(nameof(RootOptions.DiagnosticPort));

            // If there is a value for diagPortSection, then the options will obey that and disregard diagPortSection's children
            if (diagPortSection.Exists() && !string.IsNullOrEmpty(diagPortSection.Value))
            {
                options.ConnectionMode = DiagnosticPortConnectionMode.Listen;
                options.EndpointName = diagPortSection.Value;
            }
        }
    }
}
