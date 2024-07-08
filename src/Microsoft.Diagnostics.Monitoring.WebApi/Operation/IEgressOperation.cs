// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal interface IEgressOperation : IStartable
    {
        public bool IsStoppable { get; }

        public ISet<string> Tags { get; }

        public string? EgressProviderName { get; }

        public EgressProcessInfo ProcessInfo { get; }

        Task<ExecutionResult<EgressResult>> ExecuteAsync(IServiceProvider serviceProvider, CancellationToken token);

        Task StopAsync(CancellationToken token);

        void Validate(IServiceProvider serviceProvider);
    }
}
