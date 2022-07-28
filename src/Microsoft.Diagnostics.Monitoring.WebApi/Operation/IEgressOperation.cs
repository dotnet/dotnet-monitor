// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal interface IEgressOperation
    {
        public EgressProcessInfo ProcessInfo { get; }

        Task<ExecutionResult<EgressResult>> ExecuteAsync(IServiceProvider serviceProvider, CancellationToken token);

        void Validate(IServiceProvider serviceProvider);
    }
}
