// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.Extensions.Options;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options
{
    [OptionsValidator]
    sealed partial class CollectionRuleActionOptionsValidator : IValidateOptions<CollectionRuleActionOptions>
    {
    }
}
