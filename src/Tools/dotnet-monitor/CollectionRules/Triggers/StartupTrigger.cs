// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Triggers
{
    /// <summary>
    /// Trigger that indicates the collection rule actions
    /// should be run at startup of the collection rule.
    /// </summary>
    internal sealed class StartupTrigger :
        ICollectionRuleStartupTrigger
    {
        private readonly Action _callback;

        public StartupTrigger(Action callback)
        {
            _callback = callback;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _callback();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
