// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions
{
    /// <summary>
    /// Produces first chance exceptions from the current app domain.
    /// </summary>
    internal sealed class CurrentAppDomainExceptionSource :
        IExceptionSource,
        IDisposable
    {
        private long _disposedState;
        private ThreadLocal<bool> _handlingException = new();

        public event EventHandler<Exception>? ExceptionThrown;

        public CurrentAppDomainExceptionSource()
        {
            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
        }

        private void CurrentDomain_FirstChanceException(object? sender, FirstChanceExceptionEventArgs e)
        {
            if (_handlingException.Value)
            {
                // Exception handling is already in progress on this thread. The current exception is likely
                // due to the handling code and should be ignored. It will be logged (if not handled) at the
                // root call of the first chance exception handler.
                return;
            }

            // Prevent exeptions from unwinding into user code.
            try
            {
                _handlingException.Value = true;

                ExceptionThrown?.Invoke(sender, e.Exception);
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
            if (0 != Interlocked.CompareExchange(ref _disposedState, 1, 0))
                return;

            AppDomain.CurrentDomain.FirstChanceException -= CurrentDomain_FirstChanceException;
        }
    }
}
