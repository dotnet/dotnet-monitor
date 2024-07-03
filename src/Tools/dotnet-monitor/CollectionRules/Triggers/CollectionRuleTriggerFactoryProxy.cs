// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Triggers
{
    /// <summary>
    /// A proxy that allows invoking the trigger factory without
    /// having to specify a typed options instance.
    /// </summary>
    internal sealed class CollectionRuleTriggerFactoryProxy<TFactory> :
        ICollectionRuleTriggerFactoryProxy
        where TFactory : ICollectionRuleTriggerFactory
    {
        private readonly TFactory _factory;

        public CollectionRuleTriggerFactoryProxy(TFactory factory)
        {
            _factory = factory;
        }

        /// <inheritdoc/>
        public ICollectionRuleTrigger Create(IEndpointInfo endpointInfo, Action callback, object? options)
        {
            return _factory.Create(endpointInfo, callback);
        }
    }

    /// <summary>
    /// A proxy that allows invoking the trigger factory without
    /// having to specify a typed options instance.
    /// </summary>
    /// <remarks>
    /// The type of the options instance is validated before passing
    /// to the factory implementation.
    /// </remarks>
    internal sealed class CollectionRuleTriggerFactoryProxy<TFactory, TOptions> :
        ICollectionRuleTriggerFactoryProxy
        where TFactory : ICollectionRuleTriggerFactory<TOptions>
        where TOptions : class
    {
        private readonly TFactory _factory;

        public CollectionRuleTriggerFactoryProxy(TFactory factory)
        {
            _factory = factory;
        }

#nullable disable
        /// <inheritdoc/>
        public ICollectionRuleTrigger Create(IEndpointInfo endpointInfo, Action callback, object options)
        {
            // The options either need to be null or of the type that the factory expects.
            TOptions typedOptions = options as TOptions;
            if (null != options && null == typedOptions)
            {
                throw new ArgumentException(nameof(options));
            }

            return _factory.Create(endpointInfo, callback, typedOptions);
        }
#nullable restore
    }
}
