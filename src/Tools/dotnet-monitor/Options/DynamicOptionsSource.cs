using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Tools.Monitor.Options
{
    /// <summary>
    /// Class that provides the current value of a named option of type <typeparamref name="TOptions"/>
    /// and clears the options cache for this option type when a change notification is received from
    /// an <see cref="IDynamicOptionsChangeTokenSource{TOptions}"/> instance.
    /// </summary>
    /// <remarks>
    /// While most of this functionality is provided by <see cref="OptionsMonitor{TOptions}"/>, it helps
    /// in the case where the named options of type <typeparamref name="TOptions"/> are not known at
    /// service configuration time, thus preventing of proper change notification. Use this class instead
    /// and register an <see cref="IDynamicOptionsChangeTokenSource{TOptions}"/> instance for each source
    /// of configuration data that may impact the binding of <typeparamref name="TOptions"/> instances.
    /// </remarks>
    internal sealed class DynamicOptionsSource<TOptions> :
        IDynamicOptionsSource<TOptions>,
        IDisposable
        where TOptions : class
    {
        private readonly IOptionsMonitorCache<TOptions> _cache;
        private readonly IOptionsMonitor<TOptions> _monitor;
        private readonly IList<IDisposable> _registrations;

        public DynamicOptionsSource(
            IOptionsMonitorCache<TOptions> cache,
            IOptionsMonitor<TOptions> monitor,
            IEnumerable<IDynamicOptionsChangeTokenSource<TOptions>> sources)
        {
            _cache = cache;
            _monitor = monitor;
            _registrations = new List<IDisposable>();

            // Register change callbacks on each of the token sources in order to clear
            // the options cache for TOptions instances.
            foreach (IDynamicOptionsChangeTokenSource<TOptions> source in sources)
            {
                _registrations.Add(ChangeToken.OnChange(
                    () => source.GetChangeToken(),
                    () => Reset()));
            }
        }

        public void Dispose()
        {
            foreach (IDisposable registration in _registrations)
            {
                registration.Dispose();
            }
            _registrations.Clear();
        }

        public TOptions Get(string name)
        {
            return _monitor.Get(name);
        }

        private void Reset()
        {
            _cache.Clear();
        }
    }
}
