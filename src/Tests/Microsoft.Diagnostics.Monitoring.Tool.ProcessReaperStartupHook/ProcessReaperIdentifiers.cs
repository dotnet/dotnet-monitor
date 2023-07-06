// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Tools.Monitor;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    public static class ProcessReaperIdentifiers
    {

        public static class EnvironmentVariables
        {
            private const string ProcessReaperPrefix = ToolIdentifiers.StandardPrefix + "TestProcessReaper";

            public const string ParentPid = ProcessReaperPrefix + nameof(ParentPid);
        }
    }
}
