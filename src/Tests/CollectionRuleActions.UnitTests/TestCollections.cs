// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace CollectionRuleActions.UnitTests
{
    internal static class TestCollections
    {
        // This test collection is used to force all of the collection rule action tests
        // to be invoked serially in order to avoid thread pool exhaustion issues that
        // impact the ability for the collection rule actions to start asynchronously in
        // a reasonable amount of time.
        public const string CollectionRuleActions = nameof(CollectionRuleActions);
    }
}
