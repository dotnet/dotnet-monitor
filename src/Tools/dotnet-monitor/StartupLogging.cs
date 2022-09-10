// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class StartupLogging : IDisposable
    {
        private const string WindowsLocalNamedPipePrefix = @"\\.\pipe\";

        private readonly IHostApplicationLifetime _lifetime;
        private readonly ILogger _logger;
        private readonly DiagnosticPortOptions _portOptions;

        private CancellationTokenRegistration _applicationStartedRegistration;

        public StartupLogging(IHostApplicationLifetime lifetime,
            IOptions<DiagnosticPortOptions> portOptions,
            ILogger<Startup> logger)
        {
            _lifetime = lifetime;
            _logger = logger;
            _portOptions = portOptions.Value;
        }

        public void Initialize()
        {
            _applicationStartedRegistration = _lifetime.ApplicationStarted.Register(
                state => ((StartupLogging)state).Log(),
                this);
        }

        public void Dispose()
        {
            _applicationStartedRegistration.Dispose();
        }

        private void Log()
        {
            switch (_portOptions.GetConnectionMode())
            {
                case DiagnosticPortConnectionMode.Connect:
                    _logger.ConnectionModeConnect();
                    break;
                case DiagnosticPortConnectionMode.Listen:
                    string path;
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
                        !_portOptions.EndpointName.StartsWith(WindowsLocalNamedPipePrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        path = Path.Combine(WindowsLocalNamedPipePrefix, _portOptions.EndpointName);
                    }
                    else
                    {
                        path = _portOptions.EndpointName;
                    }
                    _logger.ConnectionModeListen(path);
                    break;
            }
        }
    }
}
