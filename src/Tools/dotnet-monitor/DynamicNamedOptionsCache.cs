// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    /// <summary>
    /// Custom <see cref="IOptionsMonitorCache{TOptions}"/> to override behavior
    /// for the default named options.
    /// </summary>
    /// <remarks>
    /// This cache implementation handles the default name as an indication for performing an operation
    /// on all of the names in the cache (e.g. <see cref="TryRemove(string)"/> on the default name signals
    /// that all of the named options should be cleared.
    /// </remarks>
    internal sealed class DynamicNamedOptionsCache<TOptions> :
        OptionsCache<TOptions>
        where TOptions : class
    {
        public override TOptions GetOrAdd(string name, Func<TOptions> createOptions)
        {
            // The IOptionsChangeTokenSource<TOptions> implementations will notify when the TOptions need
            // to be recomputed, however the registrations of these sources will only notify for the default
            // named option instance (because the names are not known at service configuration time). Nothing,
            // except for OptionsMonitor<TOptions> change notification, should be attempting to create/cache
            // options for the default name. In this case, just return the default value (because there is no
            // way to get a valid set of options for the default name and the value isn't used by anything).
            if (IsDefaultName(name))
            {
                return default;
            }

            return base.GetOrAdd(name, createOptions);
        }

        public override bool TryAdd(string name, TOptions options)
        {
            // The IOptionsChangeTokenSource<TOptions> implementations will notify when the TOptions need
            // to be recomputed, however the registrations of these sources will only notify for the default
            // named option instance (because the names are not known at service configuration time). Nothing
            // should be calling into this method, but handle the default name just in case.
            if (IsDefaultName(name))
            {
                Debug.Fail("Did not expect anything to attempt to cache a value for the default name.");
                return false;
            }

            return base.TryAdd(name, options);
        }

        public override bool TryRemove(string name)
        {
            // The IOptionsChangeTokenSource<TOptions> implementations will notify when the TOptions need
            // to be recomputed, however the registrations of these sources will only notify for the default
            // named option instance (because the names are not known at service configuration time). Thus,
            // interpret a removal of the default named option as a removal of all of the options instances.
            if (IsDefaultName(name))
            {
                Clear();
                return false;
            }

            return base.TryRemove(name);
        }

        private static bool IsDefaultName(string name)
        {
            return string.Equals(name, Options.DefaultName, StringComparison.Ordinal);
        }
    }
}
