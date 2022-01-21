// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;

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
                var children = diagPortSection.GetChildren();

                UpdateChild(children, nameof(DiagnosticPortOptions.ConnectionMode), nameof(DiagnosticPortConnectionMode.Listen));
                UpdateChild(children, nameof(DiagnosticPortOptions.EndpointName), diagPortSection.Value);

                options.ConnectionMode = DiagnosticPortConnectionMode.Listen;
                options.EndpointName = diagPortSection.Value;

                diagPortSection.Bind(options);
            }
        }

        private void UpdateChild(IEnumerable<IConfigurationSection> children, string childKey, string childValue)
        {
            var foundChild = children.FirstOrDefault(child => child.Key.Equals(childKey));

            if (foundChild != null)
            {
                foundChild.Value = childValue;
            }
        }
    }
}
