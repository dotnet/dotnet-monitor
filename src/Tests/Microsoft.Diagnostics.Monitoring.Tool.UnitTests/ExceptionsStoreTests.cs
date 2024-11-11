// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.WebApi.Exceptions;
using Microsoft.Diagnostics.Tools.Monitor.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    public sealed class ExceptionsStoreTests
    {
        private const ulong DefaultExceptionGroupId = 1;

        private readonly ITestOutputHelper _outputHelper;

        public ExceptionsStoreTests(ITestOutputHelper testOutput)
        {
            _outputHelper = testOutput;
        }

        /// <summary>
        /// Validates that a <see cref="ExceptionsStore"/> can be instantiated.
        /// </summary>
        [Fact]
        public async Task ExceptionsStore_Creation()
        {
            await using ExceptionsStore store = new ExceptionsStore(Array.Empty<IExceptionsStoreCallbackFactory>(), new ExceptionsConfiguration());
        }

        /// <summary>
        /// Validates that a <see cref="ExceptionsStore"/> will throw with null factories.
        /// </summary>
        [Fact]
        public void ExceptionsStore_NullFactories_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() => new ExceptionsStore(null, new ExceptionsConfiguration()));
        }

        /// <summary>
        /// Validates adding a single exception will invoke the callback
        /// and the store will contain that exception.
        /// </summary>
        [Fact]
        public async Task ExceptionsStore_AddInstance_ContainsOne()
        {
            int ExpectedCount = 1;
            ulong ExpectedId = 1;

            // Arrange
            ThresholdCallback callback = new(_outputHelper, ExpectedCount);
            await using ExceptionsStore store = new(new[] { new PassThroughCallbackFactory(callback) }, new ExceptionsConfiguration());

            IExceptionsNameCache cache = CreateCache();

            // Act
            AddExceptionInstance(store, cache, ExpectedId);

            await callback.WaitForThresholdsAsync(CommonTestTimeouts.GeneralTimeout);

            // Assert
            IReadOnlyList<IExceptionInstance> instances = store.GetSnapshot();
            IExceptionInstance instance = Assert.Single(instances);
            Assert.Equal(ExpectedId, instance.Id);
        }

        /// <summary>
        /// Validates adding multiple exceptions will invoke the callback
        /// and the store will contain those exceptions.
        /// </summary>
        [Fact]
        public async Task ExceptionsStore_AddMultipleInstance_ContainsAll()
        {
            int ExpectedCount = 3;
            ulong ExpectedId1 = 1;
            ulong ExpectedId2 = 2;
            ulong ExpectedId3 = 3;

            // Arrange
            ThresholdCallback callback = new(_outputHelper, ExpectedCount);
            await using ExceptionsStore store = new(new[] { new PassThroughCallbackFactory(callback) }, new ExceptionsConfiguration());

            IExceptionsNameCache cache = CreateCache();

            // Act
            AddExceptionInstance(store, cache, ExpectedId1);
            AddExceptionInstance(store, cache, ExpectedId2);
            AddExceptionInstance(store, cache, ExpectedId3);

            await callback.WaitForThresholdsAsync(CommonTestTimeouts.GeneralTimeout);

            // Assert
            IReadOnlyList<IExceptionInstance> instances = store.GetSnapshot();
            Assert.Equal(ExpectedCount, instances.Count);

            IExceptionInstance instance1 = instances[0];
            Assert.NotNull(instance1);
            Assert.Equal(ExpectedId1, instance1.Id);

            IExceptionInstance instance2 = instances[1];
            Assert.NotNull(instance2);
            Assert.Equal(ExpectedId2, instance2.Id);

            IExceptionInstance instance3 = instances[2];
            Assert.NotNull(instance3);
            Assert.Equal(ExpectedId3, instance3.Id);
        }

        /// <summary>
        /// Validates adding and removing exceptions will invoke the callback
        /// and the store will contain the remaining exceptions.
        /// </summary>
        [Fact]
        public async Task ExceptionsStore_AddTwoRemoveOne_ContainsOne()
        {
            ulong ExpectedId1 = 1;
            ulong ExpectedId2 = 2;

            // Arrange
            AggregateCallback aggregateCallback = new(_outputHelper);
            await using ExceptionsStore store = new(new[] { new PassThroughCallbackFactory(aggregateCallback) }, new ExceptionsConfiguration());

            IExceptionsNameCache cache = CreateCache();

            // Act
            ThresholdCallback thresholdCallback1 = new(_outputHelper, 2, 0);
            using (CallbackRegistration.Register(aggregateCallback, thresholdCallback1))
            {
                AddExceptionInstance(store, cache, ExpectedId1);
                AddExceptionInstance(store, cache, ExpectedId2);

                await thresholdCallback1.WaitForThresholdsAsync(CommonTestTimeouts.GeneralTimeout);
            }

            ThresholdCallback thresholdCallback2 = new(_outputHelper, 0, 1);
            using (CallbackRegistration.Register(aggregateCallback, thresholdCallback2))
            {
                RemoveExceptionInstance(store, ExpectedId2);

                await thresholdCallback2.WaitForThresholdsAsync(CommonTestTimeouts.GeneralTimeout);
            }

            // Assert
            IExceptionInstance instance = Assert.Single(store.GetSnapshot());
            Assert.NotNull(instance);
            Assert.Equal(ExpectedId1, instance.Id);
        }

        /// <summary>
        /// Validates adding exceptions and removing all exceptions
        /// will invoke the callback and the store will empty.
        /// </summary>
        [Fact]
        public async Task ExceptionsStore_AddThreeRemoveThree_Empty()
        {
            ulong ExpectedId1 = 1;
            ulong ExpectedId2 = 2;
            ulong ExpectedId3 = 3;

            // Arrange
            AggregateCallback aggregateCallback = new(_outputHelper);
            await using ExceptionsStore store = new(new[] { new PassThroughCallbackFactory(aggregateCallback) }, new ExceptionsConfiguration());

            IExceptionsNameCache cache = CreateCache();

            // Act
            ThresholdCallback addThreeCallback = new(_outputHelper, 3, 0);
            using (CallbackRegistration.Register(aggregateCallback, addThreeCallback))
            {
                AddExceptionInstance(store, cache, ExpectedId1);
                AddExceptionInstance(store, cache, ExpectedId2);
                AddExceptionInstance(store, cache, ExpectedId3);

                await addThreeCallback.WaitForThresholdsAsync(CommonTestTimeouts.GeneralTimeout);
            }

            ThresholdCallback removeThreeCallback = new(_outputHelper, 0, 3);
            using (CallbackRegistration.Register(aggregateCallback, removeThreeCallback))
            {
                RemoveExceptionInstance(store, ExpectedId1);
                RemoveExceptionInstance(store, ExpectedId2);
                RemoveExceptionInstance(store, ExpectedId3);

                await removeThreeCallback.WaitForThresholdsAsync(CommonTestTimeouts.GeneralTimeout);
            }

            // Assert
            Assert.Empty(store.GetSnapshot());
        }

        /// <summary>
        /// Validates adding exceptions and removing all exceptions in an unordered manner
        /// will invoke the callback and the store will be empty.
        /// </summary>
        [Fact(Skip = "Flaky")]
        public async Task ExceptionsStore_AddThreeRemoveThreeOutOfOrder_Empty()
        {
            ulong ExpectedId1 = 1;
            ulong ExpectedId2 = 2;
            ulong ExpectedId3 = 3;

            // Arrange
            AggregateCallback aggregateCallback = new(_outputHelper);
            await using ExceptionsStore store = new(new[] { new PassThroughCallbackFactory(aggregateCallback) }, new ExceptionsConfiguration());

            IExceptionsNameCache cache = CreateCache();

            // Act
            ThresholdCallback addTwoCallback = new(_outputHelper, 2, 0);
            using (CallbackRegistration.Register(aggregateCallback, addTwoCallback))
            {
                AddExceptionInstance(store, cache, ExpectedId1);
                AddExceptionInstance(store, cache, ExpectedId2);

                await addTwoCallback.WaitForThresholdsAsync(CommonTestTimeouts.GeneralTimeout);
            }

            ThresholdCallback removeOneCallback = new(_outputHelper, 0, 1);
            using (CallbackRegistration.Register(aggregateCallback, removeOneCallback))
            {
                RemoveExceptionInstance(store, ExpectedId1);

                await removeOneCallback.WaitForThresholdsAsync(CommonTestTimeouts.GeneralTimeout);
            }

            ThresholdCallback addOneCallback = new(_outputHelper, 1, 0);
            using (CallbackRegistration.Register(aggregateCallback, addOneCallback))
            {
                AddExceptionInstance(store, cache, ExpectedId3);

                await addOneCallback.WaitForThresholdsAsync(CommonTestTimeouts.GeneralTimeout);
            }

            ThresholdCallback removeTwoCallback = new(_outputHelper, 0, 2);
            using (CallbackRegistration.Register(aggregateCallback, removeTwoCallback))
            {
                RemoveExceptionInstance(store, ExpectedId2);
                RemoveExceptionInstance(store, ExpectedId3);

                await removeTwoCallback.WaitForThresholdsAsync(CommonTestTimeouts.GeneralTimeout);
            }

            // Assert
            Assert.Empty(store.GetSnapshot());
        }

        private static IExceptionsNameCache CreateCache()
        {
            EventExceptionsPipelineNameCache cache = new();
            cache.AddExceptionGroup(DefaultExceptionGroupId, 1, 1, 0);
            return cache;
        }

        private static void AddExceptionInstance(ExceptionsStore store, IExceptionsNameCache cache, ulong exceptionId)
        {
            store.AddExceptionInstance(
                cache,
                exceptionId,
                DefaultExceptionGroupId,
                null,
                DateTime.UtcNow,
                Array.Empty<ulong>(),
                0,
                Array.Empty<ulong>(),
                null,
                ActivityIdFormat.Unknown);
        }

        private static void RemoveExceptionInstance(ExceptionsStore store, ulong exceptionId)
        {
            store.RemoveExceptionInstance(exceptionId);
        }

        private sealed class PassThroughCallbackFactory : IExceptionsStoreCallbackFactory
        {
            private readonly IExceptionsStoreCallback _callback;

            public PassThroughCallbackFactory(IExceptionsStoreCallback callback)
            {
                _callback = callback ?? throw new ArgumentNullException(nameof(callback));
            }

            public IExceptionsStoreCallback Create(IExceptionsStore store)
            {
                return _callback;
            }
        }

        private sealed class ThresholdCallback : ExceptionsStoreCallbackBase
        {
            private readonly ITestOutputHelper _outputHelper;

            private readonly TaskCompletionSource _addCompletionSource =
                new(TaskCreationOptions.RunContinuationsAsynchronously);
            private readonly int _addThreshold;

            private readonly TaskCompletionSource _removeCompletionSource =
                new(TaskCreationOptions.RunContinuationsAsynchronously);
            private readonly int _removeThreshold;

            private int _addCount;
            private int _removeCount;

            public ThresholdCallback(ITestOutputHelper outputHelper, int addThreshold, int removeThreshold = 0)
            {
                _addThreshold = addThreshold;
                _removeThreshold = removeThreshold;
                _outputHelper = outputHelper;

                SetIfAtTarget(_addCompletionSource, 0, addThreshold);
                SetIfAtTarget(_removeCompletionSource, 0, removeThreshold);

                _outputHelper.WriteLine($"{nameof(ThresholdCallback)}.Ctor(Add={addThreshold}, Remove={removeThreshold})");
            }

            public override void AfterAdd(IExceptionInstance instance)
            {
                LogMember();

                if (SetIfAtTarget(_addCompletionSource, ++_addCount, _addThreshold))
                {
                    LogThresholdReached();
                }
            }

            public override void AfterRemove(IExceptionInstance instance)
            {
                LogMember();

                if (SetIfAtTarget(_removeCompletionSource, ++_removeCount, _removeThreshold))
                {
                    LogThresholdReached();
                }
            }

            public Task WaitForThresholdsAsync(TimeSpan timeout) =>
                Task.WhenAll(
                    _addCompletionSource.Task,
                    _removeCompletionSource.Task)
                .WaitAsync(timeout);

            private static bool SetIfAtTarget(TaskCompletionSource source, int value, int target)
            {
                if (value == target)
                {
                    source.SetResult();
                    return true;
                }
                return false;
            }

            private void LogMember([CallerMemberName] string memberName = "")
            {
                _outputHelper.WriteLine($"{nameof(ThresholdCallback)}.{memberName}");
            }

            private void LogThresholdReached([CallerMemberName] string memberName = "")
            {
                _outputHelper.WriteLine($"{nameof(ThresholdCallback)}.{memberName} threshold reached");
            }
        }

        private sealed class AggregateCallback : ExceptionsStoreCallbackBase
        {
            private readonly List<IExceptionsStoreCallback> _callbacks = new();

            private readonly ITestOutputHelper _outputHelper;

            public AggregateCallback(ITestOutputHelper outputHelper)
            {
                _outputHelper = outputHelper;
            }

            public void AddCallback(IExceptionsStoreCallback callback)
            {
                _callbacks.Add(callback);

                LogMember();
            }

            public void RemoveCallback(IExceptionsStoreCallback callback)
            {
                _callbacks.Remove(callback);

                LogMember();
            }

            public override void AfterAdd(IExceptionInstance instance)
            {
                LogMember();

                foreach (IExceptionsStoreCallback callback in _callbacks)
                    callback.AfterAdd(instance);
            }

            public override void AfterRemove(IExceptionInstance instance)
            {
                LogMember();

                foreach (IExceptionsStoreCallback callback in _callbacks)
                    callback.AfterRemove(instance);
            }

            public override void BeforeAdd(IExceptionInstance instance)
            {
                LogMember();

                foreach (IExceptionsStoreCallback callback in _callbacks)
                    callback.BeforeAdd(instance);
            }

            private void LogMember([CallerMemberName] string memberName = "")
            {
                _outputHelper.WriteLine($"{nameof(AggregateCallback)}.{memberName}");
            }
        }

        private sealed class CallbackRegistration : IDisposable
        {
            private readonly AggregateCallback _aggregateCallback;
            private readonly ThresholdCallback _thresholdCallback;

            private CallbackRegistration(AggregateCallback aggregateCallback, ThresholdCallback thresholdCallback)
            {
                _aggregateCallback = aggregateCallback ?? throw new ArgumentNullException(nameof(aggregateCallback));
                _thresholdCallback = thresholdCallback ?? throw new ArgumentNullException(nameof(thresholdCallback));
            }

            public void Dispose()
            {
                _aggregateCallback.RemoveCallback(_thresholdCallback);
            }

            public static IDisposable Register(AggregateCallback aggregateCallback, ThresholdCallback thresholdCallback)
            {
                aggregateCallback.AddCallback(thresholdCallback);

                return new CallbackRegistration(aggregateCallback, thresholdCallback);
            }
        }
    }
}
