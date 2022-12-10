// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Triggers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests.CollectionRules.Triggers
{
    internal sealed class ManualTriggerFactory : ICollectionRuleTriggerFactory
    {
        private readonly ManualTriggerService _service;

        public ManualTriggerFactory(ManualTriggerService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        public ICollectionRuleTrigger Create(IEndpointInfo endpointInfo, Action callback)
        {
            return new ManualTrigger(_service, callback);
        }
    }

    internal sealed class ManualTrigger : ICollectionRuleTrigger
    {
        public const string TriggerName = nameof(ManualTrigger);

        private readonly Action _callback;
        private readonly ManualTriggerService _service;

        public ManualTrigger(ManualTriggerService service, Action callback)
        {
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _service.NotifyTrigger += NotifyHandler;

            _service.NotifyStartedSubscribers();

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _service.NotifyTrigger -= NotifyHandler;

            return Task.CompletedTask;
        }

        private void NotifyHandler(object sender, EventArgs args)
        {
            _callback();
        }
    }

    internal sealed class ManualTriggerService
    {
        public event EventHandler NotifyStarted;

        public event EventHandler NotifyTrigger;

        public void NotifyStartedSubscribers()
        {
            NotifyStarted?.Invoke(this, EventArgs.Empty);
        }

        public void NotifyTriggerSubscribers()
        {
            NotifyTrigger?.Invoke(this, EventArgs.Empty);
        }
    }
}
