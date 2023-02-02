// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

#if UNITTEST
namespace Microsoft.Diagnostics.Monitoring.TestCommon
#else
using Microsoft.Diagnostics.Monitoring.WebApi;

namespace Microsoft.Diagnostics.Tools.Monitor
#endif
{
    internal class ExperimentalFlags : IExperimentalFlags
    {
        private const string ExperimentalPrefix = ToolIdentifiers.StandardPrefix + "Experimental_";

        // Feature flags
        public const string Feature_CallStacks = ExperimentalPrefix + nameof(Feature_CallStacks);
        public const string Feature_Exceptions = ExperimentalPrefix + nameof(Feature_Exceptions);

        // Behaviors
        private const string EnabledTrueValue = "True";
        private const string EnabledOneValue = "1";

        private readonly Lazy<bool> _isCallStacksEnabledLazy = new Lazy<bool>(() => IsFeatureEnabled(Feature_CallStacks));

        private readonly Lazy<bool> _isExceptionsEnabledLazy = new Lazy<bool>(() => IsFeatureEnabled(Feature_Exceptions));

        private static bool IsFeatureEnabled(string environmentVariable)
        {
            string value = Environment.GetEnvironmentVariable(environmentVariable);

            return string.Equals(EnabledTrueValue, value, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(EnabledOneValue, value, StringComparison.OrdinalIgnoreCase);
        }

        public bool IsCallStacksEnabled => _isCallStacksEnabledLazy.Value;

        public bool IsExceptionsEnabled => _isExceptionsEnabledLazy.Value;
    }
}
