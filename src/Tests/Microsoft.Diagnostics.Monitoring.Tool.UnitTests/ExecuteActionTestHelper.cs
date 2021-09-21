// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    internal class ExecuteActionTestHelper
    {
        public static string GenerateArgumentsString(string[] additionalArgs)
        {
            List<string> args = new();

            // Entrypoint assembly
            args.Add(AssemblyHelper.GetAssemblyArtifactBinPath(Assembly.GetExecutingAssembly(), "Microsoft.Diagnostics.Monitoring.ExecuteActionApp", TargetFrameworkMoniker.NetCoreApp31));

            // Entrypoint arguments
            args.AddRange(additionalArgs);

            return string.Join(' ', args);
        }
    }
}
