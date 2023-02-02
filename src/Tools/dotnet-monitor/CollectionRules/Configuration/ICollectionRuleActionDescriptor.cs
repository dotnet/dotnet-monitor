// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Configuration
{
    internal interface ICollectionRuleActionDescriptor
    {
        string ActionName { get; }

        Type FactoryType { get; }

        Type OptionsType { get; }
    }
}
