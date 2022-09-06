// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

#if UNITTEST
namespace Microsoft.Diagnostics.Monitoring.TestCommon
#else
namespace Microsoft.Diagnostics.Tools.Monitor
#endif
{
    internal static class ExperimentalFlags
    {
        private const string ExperimentalPrefix = "DotnetMonitor_Experimental_";

        // Feature flags
        public const string Feature_CallStacks = ExperimentalPrefix + nameof(Feature_CallStacks);

        // Behaviors
        private const string DisabledFalseValue = "False";
        private const string DisabledZeroValue = "0";

        private static readonly Lazy<bool> _isCallStacksEnabledLazy = new Lazy<bool>(() => IsFeatureEnabled(Feature_CallStacks));

        private static bool IsFeatureEnabled(string environmentVariable)
        {
            string value = Environment.GetEnvironmentVariable(environmentVariable);

            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            return !string.Equals(DisabledZeroValue, value, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(DisabledFalseValue, value, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsCallStacksEnabled => _isCallStacksEnabledLazy.Value;
    }
}
