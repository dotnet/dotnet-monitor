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
            Console.Error.WriteLine("THIS IS A DEBUG MESSAGE 2.5");
        }

        public void Configure(string name, DiagnosticPortOptions options)
        {
            Console.Error.WriteLine("THIS IS A DEBUG MESSAGE 3");

            IConfigurationSection diagPortSection = _configuration.GetSection(nameof(RootOptions.DiagnosticPort));
            if (diagPortSection.Exists())
            {
                diagPortSection.Bind(options);

                BindDiagnosticPortSettings(diagPortSection, options);
            }
        }

        public void Configure(DiagnosticPortOptions options)
        {
            Console.Error.WriteLine("THIS IS A DEBUG MESSAGE 3");

            IConfigurationSection diagPortSection = _configuration.GetSection(nameof(RootOptions.DiagnosticPort));
            if (diagPortSection.Exists())
            {
                diagPortSection.Bind(options);

                BindDiagnosticPortSettings(diagPortSection, options);
            }
        }

        private void BindDiagnosticPortSettings(IConfigurationSection diagPortSection, DiagnosticPortOptions dpOptions)
        {
            Console.Error.WriteLine("THIS IS A DEBUG MESSAGE 4");

            dpOptions.ConnectionMode = DiagnosticPortConnectionMode.Listen;
            dpOptions.EndpointName = diagPortSection.Value;

            //DiagnosticPortOptions options = new DiagnosticPortOptions();
            //options.ConnectionMode = DiagnosticPortConnectionMode.Listen;
            //options.EndpointName = "\\\\.\\pipe\\dotnet-monitor-pipe";



            //ruleSection.GetSection(nameof(RootOptions.DiagnosticPort)).ToString();

            diagPortSection.Bind(dpOptions);

            //dpOptions = options;

            /*
            if (null != rootOptions &&
                _triggerOperations.TryCreateOptions(triggerOptions.Type, out object triggerSettings))
            {
                IConfigurationSection diagPortSection = ruleSection.GetSection(nameof(RootOptions.DiagnosticPort));

                diagPortSection.Bind(triggerSettings);

                rootOptions.DiagnosticPort = triggerSettings;
            }
            */
        }
    }
}
