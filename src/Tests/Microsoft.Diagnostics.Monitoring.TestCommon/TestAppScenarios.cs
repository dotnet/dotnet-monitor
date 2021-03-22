// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    public static class TestAppScenarios
    {
        public static class Commands
        {
            public const string EndScenario = nameof(EndScenario);
            public const string StartScenario = nameof(StartScenario);
        }

        public static class SpinWait
        {
            public const string Name = nameof(SpinWait);

            public static class Commands
            {
                public const string Continue = nameof(Continue);
            }
        }
    }
}
