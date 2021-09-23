using System;
using System.Threading.Tasks;

#if UNITTEST
namespace Microsoft.Diagnostics.Monitoring.TestCommon
#else
namespace Microsoft.Diagnostics.Tools.Monitor
#endif
{
    internal static class DisposableHelper
    {
        /// <summary>
        /// Checks if the object implements <see cref="IAsyncDisposable"/>
        /// or <see cref="IDisposable"/> and calls the corresponding dispose method.
        /// </summary>
        public static async ValueTask DisposeAsync(object obj)
        {
            if (obj is IAsyncDisposable asyncDisposableAction)
            {
                await asyncDisposableAction.DisposeAsync();
            }
            else if (obj is IDisposable disposableAction)
            {
                disposableAction.Dispose();
            }
        }
    }
}
