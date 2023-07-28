// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.WebApi.Exceptions;
using Microsoft.Diagnostics.Tools.Monitor.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    public sealed class ConfiguredExceptionsStoreTests
    {
        private const ulong DefaultExceptionGroupId = 1;

        /// <summary>
        /// Validates that a <see cref="ConfiguredExceptionsStore"/> can be instantiated.
        /// </summary>
        [Fact]
        public async Task ConfiguredExceptionsStore_Creation()
        {
            await using ConfiguredExceptionsStore store = new ConfiguredExceptionsStore(7);
        }

        /// <summary>
        /// Validates that a <see cref="ConfiguredExceptionsStore"/> will throw with invalid top level limit.
        /// </summary>
        [Fact]
        public void ConfiguredExceptionsStore_LimitZero_ThrowsException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ConfiguredExceptionsStore(0));
        }

        /// <summary>
        /// Validates adding a single exception to the store with a limit of one will hold the exception instance.
        /// </summary>
        [Fact]
        public async Task ConfiguredExceptionsStore_LimitOne_OnlyExceptionRemains()
        {
            int ExpectedCount = 1;
            int TopLevelLimit = 1;
            ulong ExpectedId = 1;

            ThresholdCallback callback = new(ExpectedCount);
            await using ConfiguredExceptionsStore store = new ConfiguredExceptionsStore(TopLevelLimit, callback);

            IExceptionsNameCache cache = CreateCache();

            // Act
            AddExceptionInstance(store, cache, ExpectedId, Array.Empty<ulong>());

            await callback.WaitForThresholdAsync(CommonTestTimeouts.GeneralTimeout);

            // Assert
            IReadOnlyList<IExceptionInstance> instances = store.GetSnapshot();
            IExceptionInstance instance = Assert.Single(instances);
            Assert.Equal(ExpectedId, instance.Id);
        }

        /// <summary>
        /// Validates adding that the store will contain both an inner exception and outer exception, even
        /// with be top level of one (because there is only one top level exception and the inner exception
        /// is an inner exception of the outer exception).
        /// </summary>
        [Fact]
        public async Task ConfiguredExceptionsStore_LimitOne_SingleRemainsWithInnerExceptions()
        {
            int ExpectedCount = 2;
            int TopLevelLimit = 1;
            ulong InnerExceptionId = 1;
            ulong OuterExceptionId = 2;

            ThresholdCallback callback = new(ExpectedCount);
            await using ConfiguredExceptionsStore store = new ConfiguredExceptionsStore(TopLevelLimit, callback);

            IExceptionsNameCache cache = CreateCache();

            // Act
            AddExceptionInstance(store, cache, InnerExceptionId, Array.Empty<ulong>());
            AddExceptionInstance(store, cache, OuterExceptionId, new ulong[] { InnerExceptionId });

            await callback.WaitForThresholdAsync(CommonTestTimeouts.GeneralTimeout);

            // Assert
            IReadOnlyList<IExceptionInstance> instances = store.GetSnapshot();
            Assert.NotNull(instances);
            Assert.Equal(ExpectedCount, instances.Count);

            IExceptionInstance innerInstance = instances[0];
            Assert.NotNull(innerInstance);
            Assert.Equal(InnerExceptionId, innerInstance.Id);

            IExceptionInstance outerInstance = instances[1];
            Assert.NotNull(outerInstance);
            Assert.Equal(OuterExceptionId, outerInstance.Id);
        }

        /// <summary>
        /// Validates that adding a second top level exception to a store with a limit of one
        /// will remove the older exception.
        /// </summary>
        [Fact]
        public async Task ConfiguredExceptionsStore_LimitOne_LastRemains()
        {
            int ExpectedCount = 2;
            int TopLevelLimit = 1;
            ulong OuterException1Id = 1;
            ulong OuterException2Id = 2;

            ThresholdCallback callback = new(ExpectedCount);
            await using ConfiguredExceptionsStore store = new ConfiguredExceptionsStore(TopLevelLimit, callback);

            IExceptionsNameCache cache = CreateCache();

            // Act
            AddExceptionInstance(store, cache, OuterException1Id, Array.Empty<ulong>());
            AddExceptionInstance(store, cache, OuterException2Id, Array.Empty<ulong>());

            await callback.WaitForThresholdAsync(CommonTestTimeouts.GeneralTimeout);

            // Assert
            IReadOnlyList<IExceptionInstance> instances = store.GetSnapshot();
            Assert.NotNull(instances);

            IExceptionInstance outerInstance2 = Assert.Single(instances);
            Assert.Equal(OuterException2Id, outerInstance2.Id);
        }

        /// <summary>
        /// Validates that the last top level exception and its inner exceptions will remain
        /// in the store with a limit of one.
        /// </summary>
        [Fact]
        public async Task ConfiguredExceptionsStore_LimitOne_LastRemainsWithInnerExceptions()
        {
            int ExceptedInstanceCount = 4;
            int ExpectedStoreCount = 2;
            int TopLevelLimit = 1;
            ulong InnerException1Id = 1;
            ulong OuterException1Id = 2;
            ulong InnerException2Id = 3;
            ulong OuterException2Id = 4;

            ThresholdCallback callback = new(ExceptedInstanceCount);
            await using ConfiguredExceptionsStore store = new ConfiguredExceptionsStore(TopLevelLimit, callback);

            IExceptionsNameCache cache = CreateCache();

            // Act
            AddExceptionInstance(store, cache, InnerException1Id, Array.Empty<ulong>());
            AddExceptionInstance(store, cache, OuterException1Id, new ulong[] { InnerException1Id });
            AddExceptionInstance(store, cache, InnerException2Id, Array.Empty<ulong>());
            AddExceptionInstance(store, cache, OuterException2Id, new ulong[] { InnerException2Id });

            await callback.WaitForThresholdAsync(CommonTestTimeouts.GeneralTimeout);

            // Assert
            IReadOnlyList<IExceptionInstance> instances = store.GetSnapshot();
            Assert.NotNull(instances);
            Assert.Equal(ExpectedStoreCount, instances.Count);

            IExceptionInstance innerInstance2 = instances[0];
            Assert.NotNull(innerInstance2);
            Assert.Equal(InnerException2Id, innerInstance2.Id);

            IExceptionInstance outerInstance2 = instances[1];
            Assert.NotNull(outerInstance2);
            Assert.Equal(OuterException2Id, outerInstance2.Id);
        }

        /// <summary>
        /// Validates that the last top level exception that shares an inner exception with a prior
        /// top level exception remains in the store with a limit of one, including the inner exception.
        /// </summary>
        [Fact]
        public async Task ConfiguredExceptionsStore_LimitOne_LastRemainsWithSharedInnerException()
        {
            int ExceptedInstanceCount = 3;
            int ExpectedStoreCount = 2;
            int TopLevelLimit = 1;
            ulong InnerExceptionId = 1;
            ulong OuterException1Id = 2;
            ulong OuterException2Id = 3;

            ThresholdCallback callback = new(ExceptedInstanceCount);
            await using ConfiguredExceptionsStore store = new ConfiguredExceptionsStore(TopLevelLimit, callback);

            IExceptionsNameCache cache = CreateCache();

            // Act
            AddExceptionInstance(store, cache, InnerExceptionId, Array.Empty<ulong>());
            AddExceptionInstance(store, cache, OuterException1Id, new ulong[] { InnerExceptionId });
            AddExceptionInstance(store, cache, OuterException2Id, new ulong[] { InnerExceptionId });

            await callback.WaitForThresholdAsync(CommonTestTimeouts.GeneralTimeout);

            // Assert
            IReadOnlyList<IExceptionInstance> instances = store.GetSnapshot();
            Assert.NotNull(instances);
            Assert.Equal(ExpectedStoreCount, instances.Count);

            IExceptionInstance innerInstance = instances[0];
            Assert.NotNull(innerInstance);
            Assert.Equal(InnerExceptionId, innerInstance.Id);

            IExceptionInstance outerInstance2 = instances[1];
            Assert.NotNull(outerInstance2);
            Assert.Equal(OuterException2Id, outerInstance2.Id);
        }

        /// <summary>
        /// Validates that adding a third top level exception to a store with a limit of two
        /// will remove the first exception.
        /// </summary>
        [Fact]
        public async Task ConfiguredExceptionsStore_LimitTwo_LastTwoRemain()
        {
            int ExceptedInstanceCount = 3;
            int ExpectedStoreCount = 2;
            int TopLevelLimit = 2;
            ulong OuterException1Id = 1;
            ulong OuterException2Id = 2;
            ulong OuterException3Id = 3;

            ThresholdCallback callback = new(ExceptedInstanceCount);
            await using ConfiguredExceptionsStore store = new ConfiguredExceptionsStore(TopLevelLimit, callback);

            IExceptionsNameCache cache = CreateCache();

            // Act
            AddExceptionInstance(store, cache, OuterException1Id, Array.Empty<ulong>());
            AddExceptionInstance(store, cache, OuterException2Id, Array.Empty<ulong>());
            AddExceptionInstance(store, cache, OuterException3Id, Array.Empty<ulong>());

            await callback.WaitForThresholdAsync(CommonTestTimeouts.GeneralTimeout);

            // Assert
            IReadOnlyList<IExceptionInstance> instances = store.GetSnapshot();
            Assert.NotNull(instances);
            Assert.Equal(ExpectedStoreCount, instances.Count);

            IExceptionInstance outerInstance2 = instances[0];
            Assert.NotNull(outerInstance2);
            Assert.Equal(OuterException2Id, outerInstance2.Id);

            IExceptionInstance outerInstance3 = instances[1];
            Assert.NotNull(outerInstance3);
            Assert.Equal(OuterException3Id, outerInstance3.Id);
        }

        private static IExceptionsNameCache CreateCache()
        {
            EventExceptionsPipelineNameCache cache = new();
            cache.AddExceptionGroup(DefaultExceptionGroupId, 1, 1, 0);
            return cache;
        }

        private static void AddExceptionInstance(ConfiguredExceptionsStore store, IExceptionsNameCache cache, ulong exceptionId, ulong[] innerExceptionIds)
        {
            store.AddExceptionInstance(
                cache,
                exceptionId,
                DefaultExceptionGroupId,
                null,
                DateTime.UtcNow,
                Array.Empty<ulong>(),
                0,
                innerExceptionIds,
                null,
                ActivityIdFormat.Unknown);
        }

        private sealed class ThresholdCallback : ExceptionsStoreCallback
        {
            private readonly TaskCompletionSource _resultTaskSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            private readonly int _threshold;

            private int _count;

            public ThresholdCallback(int threshold)
            {
                _threshold = threshold;
            }

            public override void AfterAdd(IExceptionInstance instance)
            {
                if (++_count == _threshold)
                {
                    _resultTaskSource.SetResult();
                }
            }

            public Task WaitForThresholdAsync(TimeSpan timeout) => _resultTaskSource.Task.WaitAsync(timeout);
        }
    }
}
