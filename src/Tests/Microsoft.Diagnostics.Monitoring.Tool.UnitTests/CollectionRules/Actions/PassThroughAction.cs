// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests.CollectionRules.Actions
{
    internal class PassThroughActionFactory : ICollectionRuleActionFactory<PassThroughOptions>
    {
        public ICollectionRuleAction Create(IEndpointInfo endpointInfo, PassThroughOptions options)
        {
            return new PassThroughAction(endpointInfo, options);
        }
    }

    internal class PassThroughAction : CollectionRuleActionBase<PassThroughOptions>
    {
        public PassThroughAction(IEndpointInfo endpointInfo, PassThroughOptions settings)
            : base(endpointInfo, settings)
        {
        }

        protected override Task<CollectionRuleActionResult> ExecuteCoreAsync(TaskCompletionSource<object> startCompletionSource, CancellationToken token)
        {
            startCompletionSource.TrySetResult(null);

            CollectionRuleActionResult result = new CollectionRuleActionResult() { OutputValues = new Dictionary<string, string>() };

            result.OutputValues.Add("Output1", Options.Input1);
            result.OutputValues.Add("Output2", Options.Input2);
            result.OutputValues.Add("Output3", Options.Input3);

            return Task.FromResult(result);
        }
    }

    internal sealed class PassThroughOptions
    {
        [ActionOptionsDependencyProperty]
        public string Input1 { get; set; }

        [ActionOptionsDependencyProperty]
        public string Input2 { get; set; }

        [ActionOptionsDependencyProperty]
        public string Input3 { get; set; }
    }
}
