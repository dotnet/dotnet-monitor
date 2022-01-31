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
            yield return new object[] { "AllListen.txt", DiagnosticPortTestsConstants.AllListen_EnvironmentVariables };
        }

        internal static IHostBuilder GetDiagnosticPortHostBuilder(ITestOutputHelper outputHelper, IDictionary<string, string> diagnosticPortEnvironmentVariables)
        {
            TemporaryDirectory contentRootDirectory = new(outputHelper);
            TemporaryDirectory sharedConfigDir = new(outputHelper);
            TemporaryDirectory userConfigDir = new(outputHelper);

            // Set up the initial settings used to create the host builder.
            HostBuilderSettings settings = new()
            {
                Authentication = HostBuilderHelper.CreateAuthConfiguration(noAuth: false, tempApiKey: false),
                ContentRootDirectory = contentRootDirectory.FullName,
                SharedConfigDirectory = sharedConfigDir.FullName,
                UserConfigDirectory = userConfigDir.FullName
            };

            // Create the initial host builder.
            IHostBuilder builder = HostBuilderHelper.CreateHostBuilder(settings);

            // Override the environment configurations to use predefined values so that the test host
            // doesn't inadvertently provide unexpected values. Passing null replaces with an empty
            // in-memory collection source.
            builder.ReplaceAspnetEnvironment();
            builder.ReplaceDotnetEnvironment();
            builder.ReplaceMonitorEnvironment(diagnosticPortEnvironmentVariables);

            return builder;
        }
    }
}
