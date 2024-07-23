// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;

#if UNITTEST
namespace Microsoft.Diagnostics.Monitoring.TestCommon
#elif STARTUPHOOK
namespace Microsoft.Diagnostics.Monitoring.StartupHook
#else
namespace Microsoft.Diagnostics.Tools.Monitor
#endif
{
    public static class DisposableHelper
    {
        private const long DisposeStateActive = default(long);
        private const long DisposeStateDisposed = 1;

        /// <summary>
        /// Inspects the <paramref name="state"/> parameter to check if the object has been disposed already.
        /// </summary>
        /// <returns>
        /// True if the state has ben updated to reflect that the object has been disposed; otherwise false.
        /// </returns>
        /// <remarks>
        /// If not previously disposed, the state is updated to reflect that the object has been disposed.
        /// </remarks>
        public static bool CanDispose(ref long state)
        {
            return DisposeStateActive == Interlocked.CompareExchange(ref state, DisposeStateDisposed, DisposeStateActive);
        }

        /// <summary>
        /// Throws an <see cref="ObjectDisposedException"/> for the specified type if the state claims that the object is disposed.
        /// </summary>
        public static void ThrowIfDisposed<T>(ref long state)
        {
            ThrowIfDisposed(ref state, typeof(T));
        }

        /// <summary>
        /// Throws an <see cref="ObjectDisposedException"/> for the specified type if the state claims that the object is disposed.
        /// </summary>
        public static void ThrowIfDisposed(ref long state, Type owner)
        {
            if (IsDisposed(ref state))
            {
                throw new ObjectDisposedException(owner.FullName);
            }
        }

        public static bool IsDisposed(ref long state)
        {
            return DisposeStateDisposed == Interlocked.Read(ref state);
        }

        /// <summary>
        /// Calls <see cref="IDisposable.Dispose"/> on object if object
        /// implements <see cref="IDisposable"/> interface.
        /// </summary>
        public static void Dispose(object? obj)
        {
            if (obj is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        /// <summary>
        /// Checks if the object implements <see cref="IAsyncDisposable"/>
        /// or <see cref="IDisposable"/> and calls the corresponding dispose method.
        /// </summary>
        public static async ValueTask DisposeAsync(object? obj)
        {
            if (obj is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else
            {
                Dispose(obj);
            }
        }
    }
}
