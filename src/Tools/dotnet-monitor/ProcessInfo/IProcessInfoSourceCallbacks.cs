// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    /// <summary>
    /// Callback interface for notifications on state changes for an IProcessInfoSource implementation.
    /// </summary>
    internal interface IProcessInfoSourceCallbacks
    {
        Task OnBeforeResumeAsync(IProcessInfo processInfo, CancellationToken cancellationToken);

        Task OnAddedProcessInfoAsync(IProcessInfo processInfo, CancellationToken cancellationToken);

        void OnRemovedProcessInfo(IProcessInfo processInfo);
    }
}
