// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Extensions.Logging;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules
{
    internal class CollectionRuleContext
    {
        public CollectionRuleContext(string name, CollectionRuleOptions options, IEndpointInfo endpointInfo, ILogger logger, ISystemClock clock, Action throttledCallback = null)
        {
            // TODO: Allow null endpointInfo to allow tests to pass, but this should be provided by
            // tests since it will be required by all aspects in the future. For example, the ActionListExecutor
            // (which uses null in tests) will require this when needing to get process information for
            // the actions property bag used for token replacement.
            //EndpointInfo = endpointInfo ?? throw new ArgumentNullException(nameof(endpointInfo));
            EndpointInfo = endpointInfo;
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Options = options ?? throw new ArgumentNullException(nameof(options));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Clock = clock ?? throw new ArgumentNullException(nameof(clock));
            ThrottledCallback = throttledCallback;
        }

        public ISystemClock Clock { get; }

        public IEndpointInfo EndpointInfo { get; }

        public ILogger Logger { get; }

        public CollectionRuleOptions Options { get; }

        public string Name { get; }

        public Action ThrottledCallback { get; }
    }
}
