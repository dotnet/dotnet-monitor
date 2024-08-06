// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions
{
    /// <summary>
    /// A proxy that allows invoking the action without
    /// having to specify a typed options instance.
    /// </summary>
    internal sealed class CollectionRuleActionFactoryProxy<TFactory, TOptions> :
        ICollectionRuleActionFactoryProxy
        where TFactory : ICollectionRuleActionFactory<TOptions>
        where TOptions : class
    {
        private readonly TFactory _factory;

        public CollectionRuleActionFactoryProxy(TFactory factory)
        {
            _factory = factory;
        }

#nullable disable
        /// <inheritdoc/>
        public ICollectionRuleAction Create(IProcessInfo processInfo, object options)
        {
            TOptions typedOptions = options as TOptions;
            if (null != options && null == typedOptions)
            {
                throw new ArgumentException(nameof(options));
            }

            return _factory.Create(processInfo, typedOptions);
        }
#nullable restore
    }
}
