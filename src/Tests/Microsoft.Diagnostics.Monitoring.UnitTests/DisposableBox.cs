using System;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.UnitTests
{
    internal class DisposableBox<T> : IDisposable where T : class, IDisposable
    {
        public T Value { get; private set; }

        public DisposableBox(T value)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        public void Dispose()
        {
            Release()?.Dispose();
        }

        public async Task<U> GetAndReleaseAsync<U>(Func<T, Task<U>> func)
        {
            U value = await func(Value).ConfigureAwait(false);
            Release();
            return value;
        }

        public T Release()
        {
            T value = Value;
            Value = null;
            return value;
        }
    }
}
