// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class DiagnosticPortPostConfigureOptions :
        IPostConfigureOptions<DiagnosticPortOptions>
    {
        private readonly IConfiguration _configuration;
        private readonly IOptions<StorageOptions> _storageOptions;

        public DiagnosticPortPostConfigureOptions(
            IOptions<StorageOptions> storageOptions,
            IConfiguration configuration)
        {
            _configuration = configuration;
            _storageOptions = storageOptions;
        }

        public void PostConfigure(string? name, DiagnosticPortOptions options)
        {
            IConfigurationSection diagPortSection = _configuration.GetSection(nameof(RootOptions.DiagnosticPort));

            // If there is a value for diagPortSection, then the options will obey that and disregard diagPortSection's children
            if (diagPortSection.Exists() && !string.IsNullOrEmpty(diagPortSection.Value))
            {
                options.ConnectionMode = DiagnosticPortConnectionMode.Listen;
                options.EndpointName = diagPortSection.Value;
            }

            // Create a default server socket under the default shared path if
            // diagnostic port name was not configured but is configured for listen mode.
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
                DiagnosticPortConnectionMode.Listen == options.GetConnectionMode() &&
                string.IsNullOrEmpty(options.EndpointName) &&
                !string.IsNullOrEmpty(_storageOptions.Value.DefaultSharedPath))
            {
                options.EndpointName = Path.Combine(_storageOptions.Value.DefaultSharedPath, ToolIdentifiers.DefaultSocketName);
            }
        }
    }
}
