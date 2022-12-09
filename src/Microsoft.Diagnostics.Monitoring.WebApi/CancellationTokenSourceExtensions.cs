// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

#if UNITTEST
namespace Microsoft.Diagnostics.Monitoring.TestCommon
#else
namespace Microsoft.Diagnostics.Monitoring.WebApi
#endif
{
    internal static class CancellationTokenSourceExtensions
    {
        /// <summary>
        /// Handles all exception when calling <see cref="CancellationTokenSource.Cancel()"/>.
        /// </summary>
        public static void SafeCancel(this CancellationTokenSource source)
        {
            try
            {
                source.Cancel();
            }
            catch
            {
            }
        }
    }
}
