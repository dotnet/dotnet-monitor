// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Configuration
{
    internal sealed class CollectionRuleActionDescriptor<TAction, TOptions> :
        ICollectionRuleActionDescriptor
    {
        public CollectionRuleActionDescriptor(string actionName)
        {
            ActionName = actionName;
        }

        public string ActionName { get; }

        public Type ActionType => typeof(TAction);

        public Type OptionsType => typeof(TOptions);
    }
}
