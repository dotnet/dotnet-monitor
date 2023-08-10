// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions
{
    /// <summary>
    /// Produces the unhandled exceptions from the current app domain.
    /// </summary>
    internal sealed class CurrentAppDomainUnhandledExceptionSource :
        GuardedExceptionSourceBase,
        IDisposable
    {
        private long _disposedState;

        public CurrentAppDomainUnhandledExceptionSource()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void CurrentDomain_UnhandledException(object? sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception exception)
            {
                RaiseExceptionGuarded(exception);
            }
        }

        public void Dispose()
        {
            if (!DisposableHelper.CanDispose(ref _disposedState))
                return;

            AppDomain.CurrentDomain.UnhandledException -= CurrentDomain_UnhandledException;
        }
    }
}
