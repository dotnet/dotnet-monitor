// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal static class CollectionRuleMetadataNames
    {
        /// <summary>
        /// Represents the name of the triggered collection rule.
        /// </summary>
        public const string CollectionRuleName = nameof(CollectionRuleName);

        /// <summary>
        /// Represents the index in the action list of the currently executing action.
        /// </summary>
        public const string ActionListIndex = nameof(ActionListIndex);

        /// <summary>
        /// Represents the name of the currently executing action (when available).
        /// </summary>
        public const string ActionName = nameof(ActionName);
    }
}
