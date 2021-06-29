// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System;
using System.Threading;
using MEOOptions = Microsoft.Extensions.Options.Options;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    /// <summary>
    /// Notifies that ApiKeyAuthenticationOptions changes when ApiAuthenticationOptions changes.
    /// </summary>
    internal sealed class ApiKeyAuthenticationOptionsChangeTokenSource :
        IOptionsChangeTokenSource<ApiKeyAuthenticationOptions>,
        IDisposable
    {
        private readonly IOptionsMonitor<ApiAuthenticationOptions> _optionsMonitor;
        private readonly IDisposable _changeRegistration;

        private ConfigurationReloadToken _reloadToken = new ConfigurationReloadToken();

        public ApiKeyAuthenticationOptionsChangeTokenSource(
            IOptionsMonitor<ApiAuthenticationOptions> optionsMonitor)
        {
            _optionsMonitor = optionsMonitor;
            _changeRegistration = _optionsMonitor.OnChange(OnReload);
        }

        public string Name => MEOOptions.DefaultName;

        public IChangeToken GetChangeToken()
        {
            return _reloadToken;
        }

        public void Dispose()
        {
            _changeRegistration.Dispose();
        }

        private void OnReload(ApiAuthenticationOptions options)
        {
            Interlocked.Exchange(ref _reloadToken, new ConfigurationReloadToken()).OnReload();
        }
    }
}
