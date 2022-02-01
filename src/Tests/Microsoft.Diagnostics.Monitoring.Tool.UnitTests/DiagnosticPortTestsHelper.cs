// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    internal static class DiagnosticPortTestsHelper
    {
        public static IEnumerable<object[]> GetFileNamesAndEnvironmentVariables()
        {
            yield return new object[] { "SimplifiedListen.txt", DiagnosticPortTestsConstants.SimplifiedListen_EnvironmentVariables };
            yield return new object[] { "FullListen.txt", DiagnosticPortTestsConstants.FullListen_EnvironmentVariables };
            yield return new object[] { "Connect.txt", DiagnosticPortTestsConstants.Connect_EnvironmentVariables };
            yield return new object[] { "SimplifiedListen.txt", DiagnosticPortTestsConstants.AllListen_EnvironmentVariables };
        }
    }
}
