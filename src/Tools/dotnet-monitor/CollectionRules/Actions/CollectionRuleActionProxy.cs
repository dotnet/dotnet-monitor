// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions
{
    /// <summary>
    /// A proxy that allows invoking the action without
    /// having to specify a typed options instance.
    /// </summary>
    internal sealed class CollectionRuleActionProxy<TAction, TOptions> :
        ICollectionRuleActionProxy
        where TAction : ICollectionRuleAction<TOptions>
        where TOptions : class
    {
        private readonly TAction _action;

        public CollectionRuleActionProxy(TAction action)
        {
            _action = action;
        }

        /// <inheritdoc/>
        public Task<CollectionRuleActionResult> ExecuteAsync(object options, IProcessInfo processInfo, CancellationToken token)
        {
            TOptions typedOptions = options as TOptions;
            if (null != options && null == typedOptions)
            {
                throw new ArgumentException(nameof(options));
            }

            return _action.ExecuteAsync(typedOptions, processInfo, token);
        }
    }
}
