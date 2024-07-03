// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System;
using System.Threading;

namespace Microsoft.Diagnostics.Tools.Monitor.Auth.ApiKey
{
    /// <summary>
    /// Notifies that <see cref="MonitorApiKeyConfiguration"/> changes when <see cref="MonitorApiKeyOptions"/> changes.
    /// </summary>
    internal sealed class MonitorApiKeyChangeTokenSource :
        IOptionsChangeTokenSource<MonitorApiKeyConfiguration>,
        IDisposable
    {
        private readonly IOptionsMonitor<MonitorApiKeyOptions> _optionsMonitor;
        private readonly IDisposable? _changeRegistration;

        private ConfigurationReloadToken _reloadToken = new ConfigurationReloadToken();

        public MonitorApiKeyChangeTokenSource(
            IOptionsMonitor<MonitorApiKeyOptions> optionsMonitor)
        {
            _optionsMonitor = optionsMonitor;
            _changeRegistration = _optionsMonitor.OnChange(OnReload);
        }

        public string Name => Options.DefaultName;

        public IChangeToken GetChangeToken()
        {
            return _reloadToken;
        }

        public void Dispose()
        {
            _changeRegistration?.Dispose();
        }

        private void OnReload(MonitorApiKeyOptions options)
        {
            Interlocked.Exchange(ref _reloadToken, new ConfigurationReloadToken()).OnReload();
        }
    }
}
