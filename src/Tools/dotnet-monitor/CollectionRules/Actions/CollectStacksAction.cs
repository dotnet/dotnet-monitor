// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Exceptions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Utils = Microsoft.Diagnostics.Monitoring.WebApi.Utilities;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions
{
    internal sealed class CollectStacksActionFactory : ICollectionRuleActionFactory<CollectStacksOptions>
    {
        private readonly IServiceProvider _serviceProvider;

        public CollectStacksActionFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public ICollectionRuleAction Create(IEndpointInfo endpointInfo, CollectStacksOptions options)
        {
            if (null == options)
            {
                throw new ArgumentNullException(nameof(options));
            }

            ValidationContext context = new(options, _serviceProvider, items: null);
            Validator.ValidateObject(options, context, validateAllProperties: true);

            return new CollectStacksAction(_serviceProvider, endpointInfo, options);
        }
    }

    internal sealed class CollectStacksAction : CollectionRuleActionBase<CollectStacksOptions>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ProfilerChannel _profilerChannel;

        public CollectStacksAction(IServiceProvider serviceProvider, IEndpointInfo endpointInfo, CollectStacksOptions options) : base(endpointInfo, options)
        {
            _serviceProvider = serviceProvider;
            _profilerChannel = _serviceProvider.GetRequiredService<ProfilerChannel>();
        }

        protected override async Task<CollectionRuleActionResult> ExecuteCoreAsync(TaskCompletionSource<object> startCompletionSource, CollectionRuleMetadata collectionRuleMetadata, CancellationToken token)
        {
            bool isPlainText = Options.GetFormat() == CallStackFormat.PlainText;

            string fileName = StackUtilities.GenerateStacksFilename(EndpointInfo, isPlainText);

            KeyValueLogScope scope = Utils.CreateArtifactScope(Utils.ArtifactType_Stacks, EndpointInfo);
            EgressOperation egressOperation = new EgressOperation(
                async (outputStream, token) =>
                {
                    await StackUtilities.CollectStacksAsync(startCompletionSource, EndpointInfo, _profilerChannel, MapCallstackFormat(Options.GetFormat()), outputStream, token);
                },
                Options.Egress,
                fileName,
                EndpointInfo,
                ContentTypes.ApplicationOctetStream,
                scope,
                collectionRuleMetadata);

            ExecutionResult<EgressResult> result = await egressOperation.ExecuteAsync(_serviceProvider, token);
            if (null != result.Exception)
            {
                throw new CollectionRuleActionException(result.Exception);
            }

            return new CollectionRuleActionResult()
            {
                OutputValues = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    { CollectionRuleActionConstants.EgressPathOutputValueName, result.Result.Value }
                }
            };
        }

        private static StackFormat MapCallstackFormat(CallStackFormat format) =>
            format switch
            {
                CallStackFormat.Json => StackFormat.Json,
                CallStackFormat.PlainText => StackFormat.PlainText,
                CallStackFormat.Speedscope => StackFormat.Speedscope,
                _ => throw new InvalidOperationException()
            };
    }
}
