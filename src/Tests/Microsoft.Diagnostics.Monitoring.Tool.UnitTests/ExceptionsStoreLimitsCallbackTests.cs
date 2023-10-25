// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi.Exceptions;
using Microsoft.Diagnostics.Tools.Monitor.Exceptions;
using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    public sealed class ExceptionsStoreLimitsCallbackTests
    {
        /// <summary>
        /// Validates that a <see cref="ExceptionsStoreLimitsCallback"/> can be instantiated.
        /// </summary>
        [Fact]
        public void ExceptionsStoreLimitsCallback_Creation()
        {
            new ExceptionsStoreLimitsCallback(new TestExceptionsStore(), 7);
        }

        /// <summary>
        /// Validates that a <see cref="ExceptionsStoreLimitsCallback"/> will throw with invalid top level limit.
        /// </summary>
        [Fact]
        public void ExceptionsStoreLimitsCallback_LimitZero_ThrowsException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ExceptionsStoreLimitsCallback(new TestExceptionsStore(), 0));
        }

        /// <summary>
        /// Validates reporting a single exception with a limit of one will not impact the store.
        /// </summary>
        [Fact]
        public void ExceptionsStoreLimitsCallback_LimitOne_OnlyExceptionRemains()
        {
            int TopLevelLimit = 1;
            ulong ExpectedId = 1;

            // Arrange
            TestExceptionsStore store = new();
            ExceptionsStoreLimitsCallback callback = new(store, TopLevelLimit);

            // Act
            AddExceptionInstance(callback, ExpectedId, Array.Empty<ulong>());

            // Assert
            Assert.Empty(store.RemovedExceptionIds);
        }

        /// <summary>
        /// Validates that reporting both an inner exception and outer exception will not impact the store, even
        /// with a top level limit of one (because there is only one top level exception and the inner exception
        /// is an inner exception of the outer exception).
        /// </summary>
        [Fact]
        public void ExceptionsStoreLimitsCallback_LimitOne_SingleRemainsWithInnerExceptions()
        {
            int TopLevelLimit = 1;
            ulong InnerExceptionId = 1;
            ulong OuterExceptionId = 2;

            // Arrange
            TestExceptionsStore store = new();
            ExceptionsStoreLimitsCallback callback = new(store, TopLevelLimit);

            // Act
            AddExceptionInstance(callback, InnerExceptionId, Array.Empty<ulong>());
            AddExceptionInstance(callback, OuterExceptionId, new ulong[] { InnerExceptionId });

            // Assert
            Assert.Empty(store.RemovedExceptionIds);
        }

        /// <summary>
        /// Validates that reporting a second top level exception with a limit of one
        /// will remove the older exception from the store.
        /// </summary>
        [Fact]
        public void ExceptionsStoreLimitsCallback_LimitOne_LastRemains()
        {
            int TopLevelLimit = 1;
            ulong OuterException1Id = 1;
            ulong OuterException2Id = 2;

            // Arrange
            TestExceptionsStore store = new();
            ExceptionsStoreLimitsCallback callback = new(store, TopLevelLimit);

            // Act
            AddExceptionInstance(callback, OuterException1Id, Array.Empty<ulong>());
            AddExceptionInstance(callback, OuterException2Id, Array.Empty<ulong>());

            // Assert
            ulong removedId = Assert.Single(store.RemovedExceptionIds);
            Assert.Equal(OuterException1Id, removedId);
        }

        /// <summary>
        /// Validates that the last top level exception and its inner exceptions will remain
        /// in the store with a limit of one when reporting multiple unrelated exceptions.
        /// </summary>
        [Fact]
        public void ExceptionsStoreLimitsCallback_LimitOne_LastRemainsWithInnerExceptions()
        {
            int ExpectedRemoveCount = 2;
            int TopLevelLimit = 1;
            ulong InnerException1Id = 1;
            ulong OuterException1Id = 2;
            ulong InnerException2Id = 3;
            ulong OuterException2Id = 4;

            // Arrange
            TestExceptionsStore store = new();
            ExceptionsStoreLimitsCallback callback = new(store, TopLevelLimit);

            // Act
            AddExceptionInstance(callback, InnerException1Id, Array.Empty<ulong>());
            AddExceptionInstance(callback, OuterException1Id, new ulong[] { InnerException1Id });
            AddExceptionInstance(callback, InnerException2Id, Array.Empty<ulong>());
            AddExceptionInstance(callback, OuterException2Id, new ulong[] { InnerException2Id });

            // Assert
            Assert.Equal(ExpectedRemoveCount, store.RemovedExceptionIds.Count);
            Assert.Equal(OuterException1Id, store.RemovedExceptionIds[0]);
            Assert.Equal(InnerException1Id, store.RemovedExceptionIds[1]);
        }

        /// <summary>
        /// Validates that the last top level exception that shares an inner exception with a prior
        /// top level exception remains in the store with a limit of one, including the inner exception.
        /// The callback will remove the prior top level exception from the store.
        /// </summary>
        [Fact]
        public void ExceptionsStoreLimitsCallback_LimitOne_LastRemainsWithSharedInnerException()
        {
            int TopLevelLimit = 1;
            ulong InnerExceptionId = 1;
            ulong OuterException1Id = 2;
            ulong OuterException2Id = 3;

            // Arrange
            TestExceptionsStore store = new();
            ExceptionsStoreLimitsCallback callback = new(store, TopLevelLimit);

            // Act
            AddExceptionInstance(callback, InnerExceptionId, Array.Empty<ulong>());
            AddExceptionInstance(callback, OuterException1Id, new ulong[] { InnerExceptionId });
            AddExceptionInstance(callback, OuterException2Id, new ulong[] { InnerExceptionId });

            // Assert
            ulong removedId = Assert.Single(store.RemovedExceptionIds);
            Assert.Equal(OuterException1Id, removedId);
        }

        /// <summary>
        /// Validates that reporting a third top level exception with a limit of two
        /// will remove the first exception from the store.
        /// </summary>
        [Fact]
        public void ExceptionsStoreLimitsCallback_LimitTwo_LastTwoRemain()
        {
            int TopLevelLimit = 2;
            ulong OuterException1Id = 1;
            ulong OuterException2Id = 2;
            ulong OuterException3Id = 3;

            // Arrange
            TestExceptionsStore store = new();
            ExceptionsStoreLimitsCallback callback = new(store, TopLevelLimit);

            // Act
            AddExceptionInstance(callback, OuterException1Id, Array.Empty<ulong>());
            AddExceptionInstance(callback, OuterException2Id, Array.Empty<ulong>());
            AddExceptionInstance(callback, OuterException3Id, Array.Empty<ulong>());

            // Assert
            ulong removedId = Assert.Single(store.RemovedExceptionIds);
            Assert.Equal(OuterException1Id, removedId);
        }

        private static void AddExceptionInstance(IExceptionsStoreCallback callback, ulong exceptionId, ulong[] innerExceptionIds)
        {
            Mock<IExceptionInstance> instanceMock = new();
            instanceMock
                .Setup(instance => instance.Id)
                .Returns(exceptionId);
            instanceMock
                .Setup(instance => instance.InnerExceptionIds)
                .Returns(innerExceptionIds);

            callback.BeforeAdd(instanceMock.Object);
            callback.AfterAdd(instanceMock.Object);
        }

        private sealed class TestExceptionsStore : IExceptionsStore
        {
            public void AddExceptionInstance(IExceptionsNameCache cache, ulong exceptionId, ulong groupId, string message, DateTime timestamp, ulong[] stackFrameIds, int threadId, ulong[] innerExceptionIds, string activityId, ActivityIdFormat activityIdFormat)
                => throw new NotImplementedException();

            public IReadOnlyList<IExceptionInstance> GetSnapshot()
                => throw new NotImplementedException();

            public void RemoveExceptionInstance(ulong exceptionId)
            {
                RemovedExceptionIds.Add(exceptionId);
            }

            public List<ulong> RemovedExceptionIds = new List<ulong>();
        }
    }
}
