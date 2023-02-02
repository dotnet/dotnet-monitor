// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests
{
    /// <summary>
    /// Wrapper class that disposes a held value when disposed. Allows releasing
    /// the value without disposing it so that it can be passed onto another owner.
    /// </summary>
    internal class DisposableBox<T> : IDisposable where T : class, IDisposable
    {
        public T Value { get; private set; }

        public DisposableBox(T value)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        public void Dispose()
        {
            // Dispose the value if the box still owns the value.
            Release()?.Dispose();
        }

        /// <summary>
        /// Returns the held value and clears it from the box so that it will
        /// no longer be disposed by the box.
        /// </summary>
        public T Release()
        {
            T value = Value;
            Value = null;
            return value;
        }
    }
}
