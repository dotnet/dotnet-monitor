// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    internal static class ExecuteActionTestHelper
    {
        public static string GenerateArgumentsString(Assembly testAssembly, params string[] additionalArgs)
        {
            List<string> args = new()
            {
                // Entrypoint assembly
                AssemblyHelper.GetAssemblyArtifactBinPath(testAssembly, "Microsoft.Diagnostics.Monitoring.UnitTestApp"),
                // Add scenario name
                TestAppScenarios.Execute.Name
            };

            // Entrypoint arguments
            args.AddRange(additionalArgs);

            return string.Join(' ', args);
        }
    }
}
