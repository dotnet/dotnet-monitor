// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Globalization;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class DiagnosticPortNamedOptions :
        IConfigureOptions<DiagnosticPortOptions>
    {
        private readonly IConfiguration  _configuration;

        public DiagnosticPortNamedOptions(
            IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void Configure(DiagnosticPortOptions options)
        {
            IConfigurationSection diagPortSection = _configuration.GetSection(nameof(RootOptions.DiagnosticPort));
            if (diagPortSection.Exists() && !string.IsNullOrEmpty(diagPortSection.Value))
            {
                diagPortSection.Bind(options);

                BindDiagnosticPortSettings(diagPortSection, options);
            }
            else if (!diagPortSection.Exists())
            {
                options.ConnectionMode = DiagnosticPortConnectionMode.Connect;

                diagPortSection.Bind(options);
            }
        }

        private void BindDiagnosticPortSettings(IConfigurationSection diagPortSection, DiagnosticPortOptions dpOptions)
        {
            // NOTE: This will only be hit in the event that a string value is provided for DiagnosticPort

            dpOptions.ConnectionMode = DiagnosticPortConnectionMode.Listen;
            dpOptions.EndpointName = diagPortSection.Value;

            diagPortSection.Bind(dpOptions);
        }
    }
}
