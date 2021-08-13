// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if UNITTEST
namespace Microsoft.Diagnostics.Monitoring.TestCommon.Options
#else
using System.ComponentModel;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions
#endif
{
    internal static class ExecuteOptionsDefaults
    {
        public const bool IgnoreExitCode = false;
    }
}
