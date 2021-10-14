// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules
{
    internal class CollectionRuleContext
    {
        public CollectionRuleContext(string name, CollectionRuleOptions options, IEndpointInfo endpointInfo, ILogger logger)
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
        }

        public IEndpointInfo EndpointInfo { get; }

        public ILogger Logger { get; }

        public CollectionRuleOptions Options { get; }

        public string Name { get; }

        /// <summary>
        /// Note that we only reference named actions. As such, unnamed actions will not save their results for other actions
        /// to consume.
        /// </summary>
        public IDictionary<string, CollectionRuleActionResult> ActionResults { get; } =
            new Dictionary<string, CollectionRuleActionResult>(StringComparer.Ordinal);
    }
}
