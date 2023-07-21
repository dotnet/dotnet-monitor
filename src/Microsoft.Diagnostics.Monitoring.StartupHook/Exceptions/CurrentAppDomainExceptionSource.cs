// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions
{
    /// <summary>
    /// Produces first chance exceptions from the current app domain.
    /// </summary>
    internal sealed class CurrentAppDomainExceptionSource :
        ExceptionSourceBase,
        IDisposable
    {
        private readonly ThreadLocal<bool> _handlingException = new();

        private long _disposedState;

        public CurrentAppDomainExceptionSource()
        {
            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
        }

        private void CurrentDomain_FirstChanceException(object? sender, FirstChanceExceptionEventArgs e)
        {
            DateTime timestamp = DateTime.UtcNow;

            if (_handlingException.Value)
            {
                // Exception handling is already in progress on this thread. The current exception is likely
                // due to the handling code and should be ignored. It will be logged (if not handled) at the
                // root call of the first chance exception handler.
                return;
            }

            // Prevent exceptions from unwinding into user code.
            try
            {
                _handlingException.Value = true;

                RaiseExceptionThrown(e.Exception,
                    timestamp,
                    Activity.Current?.Id,
                    Activity.Current?.IdFormat ?? ActivityIdFormat.Unknown);
            }
            catch
            {
                // TODO: Log failure
            }
            finally
            {
                _handlingException.Value = false;
            }
        }

        public void Dispose()
        {
            if (!DisposableHelper.CanDispose(ref _disposedState))
                return;

            AppDomain.CurrentDomain.FirstChanceException -= CurrentDomain_FirstChanceException;
        }
    }
}
