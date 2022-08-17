// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Fixtures
{
    /// <summary>
    /// This fixture allows injecting of a single <see cref="AzuriteFixture"/> instance
    /// that is available to all tests in the collection. This allows providing a singleton
    /// service provider for all test invocations within the collection.
    /// </summary>
    [CollectionDefinition(Name)]
    public class AzuriteCollectionFixture : ICollectionFixture<AzuriteFixture>
    {
        public const string Name = nameof(AzuriteCollectionFixture);
    }
}
