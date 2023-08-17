// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions
{
    internal abstract class CollectionRuleEgressActionBase<TOptions> : CollectionRuleActionBase<TOptions>
    {
        protected IServiceProvider ServiceProvider { get; }

        protected EgressOperationStore EgressOperationStore { get; }


        protected CollectionRuleEgressActionBase(IServiceProvider serviceProvider, IProcessInfo processInfo, TOptions options)
            : base(processInfo, options)
        {
            ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            EgressOperationStore = ServiceProvider.GetRequiredService<EgressOperationStore>();
        }

        protected abstract EgressOperation CreateArtifactOperation(CollectionRuleMetadata collectionRuleMetadata);

        protected override async Task<CollectionRuleActionResult> ExecuteCoreAsync(CollectionRuleMetadata collectionRuleMetadata, CancellationToken token)
        {
            EgressOperation egressOperation = CreateArtifactOperation(collectionRuleMetadata);
            _ = egressOperation.Started.ContinueWith(completedTask =>
            {
                // Any failures with starting the operation should be captured by the below ExecuteOperation call.
                _ = TrySetStarted();
            }, CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

            ExecutionResult<EgressResult> result = await EgressOperationStore.ExecuteOperation(egressOperation);

            if (null != result.Exception)
            {
                throw new CollectionRuleActionException(result.Exception);
            }
            string artifactPath = result.Result.Value;

            return new CollectionRuleActionResult()
            {
                OutputValues = new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        { CollectionRuleActionConstants.EgressPathOutputValueName, artifactPath }
                    }
            };
        }
    }
}
