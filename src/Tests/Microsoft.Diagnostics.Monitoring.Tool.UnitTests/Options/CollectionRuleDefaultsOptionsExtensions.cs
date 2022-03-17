// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using System;

namespace Microsoft.Diagnostics.Monitoring.TestCommon.Options
{
    internal static partial class CollectionRuleDefaultsOptionsExtensions
    {
        public static RootOptions AddCollectionRuleDefaults(this RootOptions rootOptions, Action<CollectionRuleDefaultsOptions> callback = null)
        {
            CollectionRuleDefaultsOptions settings = new();

            callback?.Invoke(settings);

            rootOptions.CollectionRuleDefaults = settings;
            return rootOptions;
        }
    }
}
