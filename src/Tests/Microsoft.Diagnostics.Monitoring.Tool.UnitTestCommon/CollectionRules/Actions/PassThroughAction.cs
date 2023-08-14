// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    internal sealed class PassThroughActionFactory : ICollectionRuleActionFactory<PassThroughOptions>
    {
        public ICollectionRuleAction Create(IProcessInfo processInfo, PassThroughOptions options)
        {
            return new PassThroughAction(options);
        }
    }

    internal sealed class PassThroughAction : ICollectionRuleAction
    {
        private readonly PassThroughOptions _options;
        private readonly TaskCompletionSource _startCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public PassThroughAction(PassThroughOptions options)
        {
            _options = options;
        }

        public Task Started => _startCompletionSource.Task;

        public Task StartAsync(CollectionRuleMetadata collectionRuleMetadata, CancellationToken token)
        {
            return StartAsync(token);
        }

        public Task StartAsync(CancellationToken token)
        {
            _startCompletionSource.TrySetResult();
            return Task.CompletedTask;
        }

        public Task<CollectionRuleActionResult> WaitForCompletionAsync(CancellationToken token)
        {
            CollectionRuleActionResult result = new CollectionRuleActionResult() { OutputValues = new Dictionary<string, string>() };

            result.OutputValues.Add("Output1", _options.Input1);
            result.OutputValues.Add("Output2", _options.Input2);
            result.OutputValues.Add("Output3", _options.Input3);

            return Task.FromResult(result);
        }
    }

    internal sealed record class PassThroughOptions : BaseRecordOptions
    {
        [ActionOptionsDependencyProperty]
        public string Input1 { get; set; }

        [ActionOptionsDependencyProperty]
        public string Input2 { get; set; }

        [ActionOptionsDependencyProperty]
        public string Input3 { get; set; }
    }
}
