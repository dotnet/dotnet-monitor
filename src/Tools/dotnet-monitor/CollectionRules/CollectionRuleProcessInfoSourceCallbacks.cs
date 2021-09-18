// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules
{
    internal class CollectionRuleProcessInfoSourceCallbacks :
        IProcessInfoSourceCallbacks
    {
        private readonly CollectionRuleService _service;

        public CollectionRuleProcessInfoSourceCallbacks(CollectionRuleService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        public Task OnAddedProcessInfoAsync(IProcessInfo processInfo, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task OnBeforeResumeAsync(IProcessInfo processInfo, CancellationToken cancellationToken)
        {
            return _service.ApplyRules(processInfo, cancellationToken);
        }

        public void OnRemovedProcessInfo(IProcessInfo processInfo)
        {
        }
    }
}
