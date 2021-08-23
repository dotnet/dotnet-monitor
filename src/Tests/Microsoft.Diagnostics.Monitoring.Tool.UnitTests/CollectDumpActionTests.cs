// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using System.Threading;
using System;
using System.IO;
using System.Globalization;
using Xunit.Abstractions;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Fixtures;
using Microsoft.Extensions.Logging;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    public sealed class CollectDumpActionTests
    {
        private const int TokenTimeoutMs = 60000;
        //private const int DelayMs = 1000;

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITestOutputHelper _outputHelper;
        private readonly IServiceProvider _serviceProvider;
        // Not sure how to hook into Services for Singleton executor and logger, so currently just making one for the tests
        private ILogger<CollectDumpAction> _logger = new Logger<CollectDumpAction>(new LoggerFactory());

        public CollectDumpActionTests(ITestOutputHelper outputHelper, ServiceProviderFixture serviceProviderFixture)
        {
            _httpClientFactory = serviceProviderFixture.ServiceProvider.GetService<IHttpClientFactory>();
            _outputHelper = outputHelper;
            _serviceProvider = serviceProviderFixture.ServiceProvider;
        }

        [Fact]
        public async Task CollectDumpAction_NoEgressProvider()
        {
            CollectDumpAction action = new(_logger, _serviceProvider);

            CollectDumpOptions options = new();

            options.Egress = null;

            using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TokenTimeoutMs);

            IEndpointInfo endpointInfo; // Need to figure out how to actually get the IEndpointInfo

            CollectionRuleActionResult result = await action.ExecuteAsync(options, endpointInfo, cancellationTokenSource.Token);

            string egressPath = result.OutputValues["EgressPath"];

            if (!File.Exists(egressPath))
            {
                throw new FileNotFoundException(string.Format(CultureInfo.InvariantCulture, Tools.Monitor.Strings.ErrorMessage_FileNotFound, egressPath));
            }

            //ValidateActionResult(result, "0");
        }
    }
}
