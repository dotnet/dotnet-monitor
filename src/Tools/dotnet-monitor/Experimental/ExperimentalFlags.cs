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
#pragma warning disable CA1823 // Avoid unused private fields
        private const string ExperimentalPrefix = ToolIdentifiers.StandardPrefix + "Experimental_";
#pragma warning restore CA1823 // Avoid unused private fields

        // Behaviors
        private const string EnabledTrueValue = "True";
        private const string EnabledOneValue = "1";

        private static bool IsFeatureEnabled(string environmentVariable)
        {
            string value = Environment.GetEnvironmentVariable(environmentVariable);

            return string.Equals(EnabledTrueValue, value, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(EnabledOneValue, value, StringComparison.OrdinalIgnoreCase);
        }
    }
}
