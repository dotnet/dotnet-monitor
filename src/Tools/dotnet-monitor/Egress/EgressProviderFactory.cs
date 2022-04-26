// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor.Egress.Configuration;
using Microsoft.Diagnostics.Tracing.Parsers.IIS_Trace;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress
{
    internal class EgressProviderFactory<TOptions> 
        where TOptions : class
    {
        public readonly string _name;
        private EgressProviderConfigurationProvider<TOptions> _configurationProvider;

        public EgressProviderFactory(string name)
        {
            this._name = name;
        }

        public EgressProviderConfigurationProvider<TOptions> Manufacture(IServiceProvider serviceProvider)
        {
            if (this._configurationProvider == null)
            {
                this._configurationProvider = new EgressProviderConfigurationProvider<TOptions>(serviceProvider.GetRequiredService<IConfiguration>(), this._name);
            }
            return this._configurationProvider;
        }
    }
}