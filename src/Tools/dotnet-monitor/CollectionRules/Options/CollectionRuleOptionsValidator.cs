// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Microsoft.Extensions.Options;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options
{
    [OptionsValidator]
    partial class CollectionRuleOptionsValidator : IValidateOptions<CollectionRuleOptions>
    {
    }
}
