// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions
{
    /// <summary>
    /// Forces options to have record semantics. This is an alternative to forcing ICloneable on action options
    /// that we may want to perform token substitution on.
    /// </summary>
    internal record class BaseRecordOptions
    {
    }
}
