// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules
{
    internal sealed class HostInfo
    {
        public HostInfo(string hostname, TimeProvider timeProvider)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(hostname);
            ArgumentNullException.ThrowIfNull(timeProvider);

            HostName = hostname;
            TimeProvider = timeProvider;
        }

        public string HostName { get; }

        public TimeProvider TimeProvider { get; }

        public static HostInfo GetCurrent(TimeProvider? timeProvider = null) =>
            new HostInfo(Dns.GetHostName(), timeProvider ?? TimeProvider.System);
    }
}
