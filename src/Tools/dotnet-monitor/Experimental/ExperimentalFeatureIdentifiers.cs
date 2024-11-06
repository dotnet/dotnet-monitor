// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal static class ExperimentalFeatureIdentifiers
    {
        public static class EnvironmentVariables
        {
            private const string Prefix = ToolIdentifiers.StandardPrefix + "Experimental_";

            public const string ServerEndpointPruningAlgorithmV2 = Prefix + nameof(ServerEndpointPruningAlgorithmV2);
        }
    }
}
