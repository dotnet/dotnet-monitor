// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Validation;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Configuration;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions
{
    internal sealed partial class SetEnvironmentVariableActionFactory :
        ICollectionRuleActionFactory<SetEnvironmentVariableOptions>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SetEnvironmentVariableActionFactory> _logger;
        private readonly ValidationOptions _validationOptions;

        public SetEnvironmentVariableActionFactory(IServiceProvider serviceProvider, ILogger<SetEnvironmentVariableActionFactory> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _validationOptions = serviceProvider.GetRequiredService<IOptions<ValidationOptions>>().Value;
        }

        public ICollectionRuleAction Create(IProcessInfo processInfo, SetEnvironmentVariableOptions options)
        {
            if (null == options)
            {
                throw new ArgumentNullException(nameof(options));
            }

            ValidationHelper.ValidateObject(options, typeof(SetEnvironmentVariableOptions), _validationOptions, _serviceProvider);

            return new SetEnvironmentVariableAction(_logger, processInfo, options);
        }

        internal sealed partial class SetEnvironmentVariableAction :
            CollectionRuleActionBase<SetEnvironmentVariableOptions>
        {
            private readonly ILogger _logger;
            private readonly SetEnvironmentVariableOptions _options;

            public SetEnvironmentVariableAction(ILogger logger, IProcessInfo processInfo, SetEnvironmentVariableOptions options)
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

                _logger.SettingEnvironmentVariable(_options.Name, EndpointInfo.ProcessId);
                await client.SetEnvironmentVariableAsync(_options.Name, _options.Value, token);

                if (!TrySetStarted())
                {
                    throw new InvalidOperationException();
                }

                return new CollectionRuleActionResult();
            }
        }
    }

    internal sealed class SetEnvironmentVariableActionDescriptor : ICollectionRuleActionDescriptor
    {
        public string ActionName => KnownCollectionRuleActions.SetEnvironmentVariable;
        public Type FactoryType => typeof(SetEnvironmentVariableActionFactory);
        public Type OptionsType => typeof(SetEnvironmentVariableOptions);

        public void BindOptions(IConfigurationSection settingsSection, out object settings)
        {
            SetEnvironmentVariableOptions options = new();
            settingsSection.Bind(options);
            settings = options;
        }
    }
}
