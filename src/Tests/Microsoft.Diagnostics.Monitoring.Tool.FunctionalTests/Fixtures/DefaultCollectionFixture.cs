// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Fixtures
{
    /// <summary>
    /// This fixture allows injecting of a single <see cref="ServiceProviderFixture"/> instance
    /// that is available to all tests in the collection. This allows providing a singleton
    /// service provider for all test invocations within the collection.
    /// </summary>
    [CollectionDefinition(Name)]
    public class DefaultCollectionFixture : ICollectionFixture<ServiceProviderFixture>
    {
        public const string Name = nameof(DefaultCollectionFixture);
    }
}
