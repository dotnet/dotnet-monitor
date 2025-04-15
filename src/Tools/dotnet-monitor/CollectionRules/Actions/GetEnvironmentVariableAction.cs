// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Validation;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions
{
    internal sealed partial class GetEnvironmentVariableActionFactory :
        ICollectionRuleActionFactory<GetEnvironmentVariableOptions>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<GetEnvironmentVariableActionFactory> _logger;
        private readonly ValidationOptions _validationOptions;

        public GetEnvironmentVariableActionFactory(IServiceProvider serviceProvider, ILogger<GetEnvironmentVariableActionFactory> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(serviceProvider));
            _validationOptions = serviceProvider.GetRequiredService<IOptions<ValidationOptions>>().Value;
        }

        public ICollectionRuleAction Create(IProcessInfo processInfo, GetEnvironmentVariableOptions options)
        {
            if (null == options)
            {
                throw new ArgumentNullException(nameof(options));
            }

            ValidationHelper.ValidateObject(options, typeof(GetEnvironmentVariableOptions), _validationOptions, _serviceProvider);

            return new GetEnvironmentVariableAction(_logger, processInfo, options);
        }

        internal sealed partial class GetEnvironmentVariableAction :
            CollectionRuleActionBase<GetEnvironmentVariableOptions>
        {
            private readonly ILogger _logger;
            private readonly GetEnvironmentVariableOptions _options;


            public GetEnvironmentVariableAction(ILogger logger, IProcessInfo processInfo, GetEnvironmentVariableOptions options)
                : base(processInfo, options)
            {
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
                _options = options ?? throw new ArgumentNullException(nameof(options));
            }

            protected override async Task<CollectionRuleActionResult> ExecuteCoreAsync(
                CollectionRuleMetadata? collectionRuleMetadata,
                CancellationToken token)
            {
                DiagnosticsClient client = new DiagnosticsClient(EndpointInfo.Endpoint);

                _logger.GettingEnvironmentVariable(_options.Name, EndpointInfo.ProcessId);
                Dictionary<string, string> envBlock = await client.GetProcessEnvironmentAsync(token);
                if (!envBlock.TryGetValue(Options.Name, out string? value))
                {
                    throw new InvalidOperationException(
                            string.Format(
                                Strings.ErrorMessage_NoEnvironmentVariable,
                                Options.Name));
                }

                if (!TrySetStarted())
                {
                    throw new InvalidOperationException();
                }

                return new CollectionRuleActionResult()
                {
                    OutputValues = new Dictionary<string, string>()
                        {
                            { CollectionRuleActionConstants.EnvironmentVariableValueName, value ?? string.Empty }
                        }
                };
            }
        }
    }
}
