// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    internal class ProcessInfoUtilities
    {
        private readonly ITestOutputHelper _outputHelper;

        private static readonly TimeSpan GetProcessInfoTimeout = TimeSpan.FromSeconds(10);

        public ProcessInfoUtilities(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        public ServerProcessInfoSource CreateServerSource(out string transportName, ProcessInfoSourceCallback callback = null)
        {
            DiagnosticPortHelper.Generate(DiagnosticPortConnectionMode.Listen, out _, out transportName);
            _outputHelper.WriteLine("Starting server endpoint info source at '" + transportName + "'.");

            List<IProcessInfoSourceCallbacks> callbacks = new();
            if (null != callback)
            {
                callbacks.Add(callback);
            }
            return new ServerProcessInfoSource(transportName, callbacks);
        }

        public AppRunner CreateAppRunner(string transportName, TargetFrameworkMoniker tfm, int appId = 1)
        {
            AppRunner appRunner = new(_outputHelper, Assembly.GetExecutingAssembly(), appId, tfm);
            appRunner.ConnectionMode = DiagnosticPortConnectionMode.Connect;
            appRunner.DiagnosticPortPath = transportName;
            appRunner.ScenarioName = TestAppScenarios.AsyncWait.Name;
            return appRunner;
        }

        public async Task<IEnumerable<IProcessInfo>> GetProcessInfoAsync(ServerProcessInfoSource source)
        {
            _outputHelper.WriteLine("Getting process infos.");
            using CancellationTokenSource cancellationSource = new(GetProcessInfoTimeout);
            return await source.GetProcessInfoAsync(cancellationSource.Token);
        }

        /// <summary>
        /// Verifies basic information on the connection and that it matches the target process from the runner.
        /// </summary>
        public static async Task VerifyConnectionAsync(AppRunner runner, IProcessInfo processInfo)
        {
            Assert.NotNull(runner);
            Assert.NotNull(processInfo);
            Assert.Equal(await runner.ProcessIdTask, processInfo.ProcessId);
            Assert.NotEqual(Guid.Empty, processInfo.RuntimeInstanceCookie);
            Assert.NotNull(processInfo.Endpoint);
        }
    }
}
