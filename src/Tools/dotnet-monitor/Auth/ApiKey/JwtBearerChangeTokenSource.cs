// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System;
using System.Threading;

namespace Microsoft.Diagnostics.Tools.Monitor.Auth.ApiKey
{
    /// <summary>
    /// Notifies that JwtBearerOptions changes when MonitorApiKeyConfiguration changes.
    /// </summary>
    internal sealed class JwtBearerChangeTokenSource :
        IOptionsChangeTokenSource<JwtBearerOptions>,
        IDisposable
    {
        private readonly IOptionsMonitor<MonitorApiKeyConfiguration> _optionsMonitor;
        private readonly IDisposable? _changeRegistration;

        private ConfigurationReloadToken _reloadToken = new ConfigurationReloadToken();

        public JwtBearerChangeTokenSource(
            IOptionsMonitor<MonitorApiKeyConfiguration> optionsMonitor)
        {
            _optionsMonitor = optionsMonitor;
            _changeRegistration = _optionsMonitor.OnChange(OnReload);
        }

        /// <summary>
        /// Returns Named config instance. <see cref="JwtBearerOptions" /> expects
        /// its configuration to be named after the AuthenticationScheme it's using, not <see cref="Options.DefaultName"/>.
        /// </summary>
        public string Name => JwtBearerDefaults.AuthenticationScheme;

        public IChangeToken GetChangeToken()
        {
            return _reloadToken;
        }

        public void Dispose()
        {
            _changeRegistration?.Dispose();
        }

        private void OnReload(MonitorApiKeyConfiguration options)
        {
            Interlocked.Exchange(ref _reloadToken, new ConfigurationReloadToken()).OnReload();
        }
    }
}
