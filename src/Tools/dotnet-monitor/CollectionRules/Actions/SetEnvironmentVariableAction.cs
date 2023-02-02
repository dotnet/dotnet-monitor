// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Exceptions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions
{
    internal sealed partial class SetEnvironmentVariableActionFactory :
        ICollectionRuleActionFactory<SetEnvironmentVariableOptions>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SetEnvironmentVariableActionFactory> _logger;

        public SetEnvironmentVariableActionFactory(IServiceProvider serviceProvider, ILogger<SetEnvironmentVariableActionFactory> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public ICollectionRuleAction Create(IEndpointInfo endpointInfo, SetEnvironmentVariableOptions options)
        {
            if (null == options)
            {
                throw new ArgumentNullException(nameof(options));
            }

            ValidationContext context = new(options, _serviceProvider, items: null);
            Validator.ValidateObject(options, context, validateAllProperties: true);

            return new SetEnvironmentVariableAction(_logger, endpointInfo, options);
        }

        internal sealed partial class SetEnvironmentVariableAction :
            CollectionRuleActionBase<SetEnvironmentVariableOptions>
        {
            private readonly IEndpointInfo _endpointInfo;
            private readonly ILogger _logger;
            private readonly SetEnvironmentVariableOptions _options;

            public SetEnvironmentVariableAction(ILogger logger, IEndpointInfo endpointInfo, SetEnvironmentVariableOptions options)
                : base(endpointInfo, options)
            {
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
                _endpointInfo = endpointInfo ?? throw new ArgumentNullException(nameof(endpointInfo));
                _options = options ?? throw new ArgumentNullException(nameof(options));
            }

            protected override async Task<CollectionRuleActionResult> ExecuteCoreAsync(
                TaskCompletionSource<object> startCompletionSource,
                CollectionRuleMetadata collectionRuleMetadata,
                CancellationToken token)
            {
                try
                {
                    DiagnosticsClient client = new DiagnosticsClient(_endpointInfo.Endpoint);

                    _logger.SettingEnvironmentVariable(_options.Name, _endpointInfo.ProcessId);
                    await client.SetEnvironmentVariableAsync(_options.Name, _options.Value, token);

                    if (!startCompletionSource.TrySetResult(null))
                    {
                        throw new InvalidOperationException();
                    }

                    return new CollectionRuleActionResult();
                }
                catch (Exception ex)
                {
                    throw new CollectionRuleActionException(ex);
                }
            }
        }
    }
}
