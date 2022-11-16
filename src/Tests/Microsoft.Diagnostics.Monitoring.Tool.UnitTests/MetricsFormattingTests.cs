// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    public sealed class MetricsFormattingTests
    {
        private ITestOutputHelper _outputHelper;

        public MetricsFormattingTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Fact]
        public async Task HistogramFormat_Test()
        {
            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions => { }, host =>
            {
                IOptionsMonitor<DiagnosticPortOptions> options = host.Services.GetService<IOptionsMonitor<DiagnosticPortOptions>>();

                Assert.Equal(DiagnosticPortTestsConstants.SimplifiedDiagnosticPort, options.CurrentValue.EndpointName);
                Assert.Equal(DiagnosticPortConnectionMode.Listen, options.CurrentValue.ConnectionMode);

            }, overrideSource: GetConfigurationSources(DiagnosticPortTestsConstants.SimplifiedListen_EnvironmentVariables));
        }
    }
}
