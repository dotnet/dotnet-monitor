// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.ExceptionServices;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions
{
    /// <summary>
    /// Produces first chance exceptions from the current app domain.
    /// </summary>
    internal sealed class CurrentAppDomainFirstChanceExceptionSource :
        GuardedExceptionSourceBase,
        IDisposable
    {
        private long _disposedState;

        public CurrentAppDomainFirstChanceExceptionSource()
        {
            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
        }

        private void CurrentDomain_FirstChanceException(object? sender, FirstChanceExceptionEventArgs e)
        {
            RaiseExceptionGuarded(e.Exception);
        }

        public void Dispose()
        {
            if (!DisposableHelper.CanDispose(ref _disposedState))
                return;

            AppDomain.CurrentDomain.FirstChanceException -= CurrentDomain_FirstChanceException;
        }
    }
}
