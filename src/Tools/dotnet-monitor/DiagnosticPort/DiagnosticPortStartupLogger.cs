// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.IO;

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Logging;
using System;

using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class DiagnosticPortStartupLogger :
        IStartupLogger
    {
        private const string WindowsLocalNamedPipePrefix = @"\\.\pipe\";

        private readonly ILogger<Startup> _logger;
        private readonly DiagnosticPortOptions _options;

        public DiagnosticPortStartupLogger(
            IOptions<DiagnosticPortOptions> options,
            ILogger<Startup> logger)
        {
            _logger = logger;
            _options = options.Value;
        }

        public void Log()
        {
            switch (_options.GetConnectionMode())
            {
                case DiagnosticPortConnectionMode.Connect:
                    _logger.ConnectionModeConnect();
                    break;
                case DiagnosticPortConnectionMode.Listen:
                    string path;
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
                        !_options.EndpointName.StartsWith(WindowsLocalNamedPipePrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        path = Path.Combine(WindowsLocalNamedPipePrefix, _options.EndpointName);
                    }
                    else
                    {
                        path = _options.EndpointName;
                    }
                    _logger.ConnectionModeListen(path);
                    break;
            }
        }
    }
}
