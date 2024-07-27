// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Extensions.Logging;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules
{
    internal class CollectionRuleContext
    {
        public CollectionRuleContext(string name, CollectionRuleOptions options, IProcessInfo processInfo, HostInfo hostInfo, ILogger logger, Action? throttledCallback = null)
        {
            // TODO: Allow null processInfo to allow tests to pass, but this should be provided by
            // tests since it will be required by all aspects in the future. For example, the ActionListExecutor
            // (which uses null in tests) will require this when needing to get process information for
            // the actions property bag used for token replacement.
            //ProcessInfo = processInfo ?? throw new ArgumentNullException(nameof(processInfo));
            ProcessInfo = processInfo;
            HostInfo = hostInfo ?? throw new ArgumentNullException(nameof(hostInfo));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Options = options ?? throw new ArgumentNullException(nameof(options));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            ThrottledCallback = throttledCallback;
        }

        public IProcessInfo ProcessInfo { get; }

        public IEndpointInfo EndpointInfo => ProcessInfo.EndpointInfo;

        public HostInfo HostInfo { get; }

        public ILogger Logger { get; }

        public CollectionRuleOptions Options { get; }

        public string Name { get; }

        public Action? ThrottledCallback { get; }
    }
}
