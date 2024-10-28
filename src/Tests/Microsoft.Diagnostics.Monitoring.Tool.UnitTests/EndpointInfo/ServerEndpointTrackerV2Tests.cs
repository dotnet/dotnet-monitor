// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests.EndpointInfo
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public sealed class ServerEndpointTrackerV2Tests
    {
        private readonly MockTimeProvider _timeProvider = new();
        private readonly IOptions<DiagnosticPortOptions> _diagPortOptions = Extensions.Options.Options.Create(new DiagnosticPortOptions()
        {
            ConnectionMode = DiagnosticPortConnectionMode.Listen
        });

        private static IServerEndpointStateChecker CreateStateChecker(params ServerEndpointState[] stateCheckResults)
        {
            Assert.NotEmpty(stateCheckResults);
            Mock<IServerEndpointStateChecker> stateChecker = new();
            var sequence = stateChecker.SetupSequence(s => s.GetEndpointStateAsync(It.IsAny<IEndpointInfo>(), It.IsAny<CancellationToken>()));

            for (int i = 0; i < stateCheckResults.Length; i++)
            {
                sequence.ReturnsAsync(stateCheckResults[i]);
            }

            sequence.ThrowsAsync(new InvalidOperationException("End of test"));

            return stateChecker.Object;
        }

        private ServerEndpointTrackerV2 CreateTracker(ServerEndpointState stateCheckResult = ServerEndpointState.Active)
            => new ServerEndpointTrackerV2(CreateStateChecker(stateCheckResult), _timeProvider, _diagPortOptions);

        private ServerEndpointTrackerV2 CreateTracker(IServerEndpointStateChecker stateChecker)
            => new ServerEndpointTrackerV2(stateChecker, _timeProvider, _diagPortOptions);


        [Fact]
        public async Task Add_RegistersEndpoint()
        {
            // Arrange
            using CancellationTokenSource cts = new CancellationTokenSource(CommonTestTimeouts.GeneralTimeout);
            using ServerEndpointTrackerV2 tracker = CreateTracker();
            IEndpointInfo endpoint = Mock.Of<IEndpointInfo>();

            // Act
            await tracker.AddAsync(endpoint, cts.Token);

            // Assert
            IEnumerable<IEndpointInfo> activeEndpoints = await tracker.GetEndpointInfoAsync(cts.Token);
            IEndpointInfo activeEndpoint = Assert.Single(activeEndpoints);
            Assert.Equal(endpoint, activeEndpoint);
        }

        [Fact]
        public async Task PruneEndpointsAsync_Prunes_ErrorEndpoint()
        {
            // Arrange
            using CancellationTokenSource cts = new CancellationTokenSource(CommonTestTimeouts.GeneralTimeout);
            using ServerEndpointTrackerV2 tracker = CreateTracker(ServerEndpointState.Error);

            IEndpointInfo endpoint = Mock.Of<IEndpointInfo>();
            await tracker.AddAsync(endpoint, cts.Token);

            EndpointRemovedEventArgs endpointRemovedArgs = null;
            tracker.EndpointRemoved += (_, args) => endpointRemovedArgs = args;

            // Act
            await tracker.PruneEndpointsAsync(cts.Token);

            // Assert
            IEnumerable<IEndpointInfo> activeEndpoints = await tracker.GetEndpointInfoAsync(cts.Token);
            Assert.Empty(activeEndpoints);

            Assert.NotNull(endpointRemovedArgs);
            Assert.Equal(ServerEndpointState.Error, endpointRemovedArgs.State);
            Assert.Equal(endpoint, endpointRemovedArgs.Endpoint);
        }

        [Fact]
        public async Task PruneEndpointsAsync_DoesNotPrune_ActiveEndpoint()
        {
            // Arrange
            using CancellationTokenSource cts = new CancellationTokenSource(CommonTestTimeouts.GeneralTimeout);
            using ServerEndpointTrackerV2 tracker = CreateTracker(ServerEndpointState.Active);
            await tracker.AddAsync(Mock.Of<IEndpointInfo>(), cts.Token);

            EndpointRemovedEventArgs endpointRemovedArgs = null;
            tracker.EndpointRemoved += (_, args) => endpointRemovedArgs = args;

            // Act
            await tracker.PruneEndpointsAsync(cts.Token);

            // Assert
            IEnumerable<IEndpointInfo> activeEndpoints = await tracker.GetEndpointInfoAsync(cts.Token);
            Assert.NotEmpty(activeEndpoints);

            Assert.Null(endpointRemovedArgs);
        }

        [Fact]
        public async Task PruneEndpointsAsync_DoesNotPrune_TransientlyUnresponsiveEndpoint()
        {
            // Arrange
            ServerEndpointState[] states = [ServerEndpointState.Active, ServerEndpointState.Unresponsive, ServerEndpointState.Active];

            using CancellationTokenSource cts = new CancellationTokenSource(CommonTestTimeouts.GeneralTimeout);
            using ServerEndpointTrackerV2 tracker = CreateTracker(CreateStateChecker(states));

            IEndpointInfo endpoint = Mock.Of<IEndpointInfo>();
            await tracker.AddAsync(endpoint, cts.Token);

            EndpointRemovedEventArgs endpointRemovedArgs = null;
            tracker.EndpointRemoved += (_, args) => endpointRemovedArgs = args;

            // Act & Assert
            for (int i = 0; i < states.Length; i++)
            {
                _timeProvider.Increment(ServerEndpointTrackerV2.UnresponsiveGracePeriod / 2);
                await tracker.PruneEndpointsAsync(cts.Token);

                IEnumerable<IEndpointInfo> activeEndpoints = await tracker.GetEndpointInfoAsync(cts.Token);
                IEndpointInfo activeEndpoint = Assert.Single(activeEndpoints);
                Assert.Equal(endpoint, activeEndpoint);
            }

            Assert.Null(endpointRemovedArgs);
        }

        [Fact]
        public async Task PruneEndpointsAsync_Prunes_UnresponsiveEndpointAfterGracePeriod()
        {
            // Arrange
            using CancellationTokenSource cts = new CancellationTokenSource(CommonTestTimeouts.GeneralTimeout);
            using ServerEndpointTrackerV2 tracker = CreateTracker(ServerEndpointState.Unresponsive);

            IEndpointInfo endpoint = Mock.Of<IEndpointInfo>();
            await tracker.AddAsync(endpoint, cts.Token);

            EndpointRemovedEventArgs endpointRemovedArgs = null;
            tracker.EndpointRemoved += (_, args) => endpointRemovedArgs = args;

            _timeProvider.Increment(ServerEndpointTrackerV2.UnresponsiveGracePeriod * 2);

            // Act
            await tracker.PruneEndpointsAsync(cts.Token);

            // Assert
            IEnumerable<IEndpointInfo> activeEndpoints = await tracker.GetEndpointInfoAsync(cts.Token);
            Assert.Empty(activeEndpoints);

            Assert.NotNull(endpointRemovedArgs);
            Assert.Equal(ServerEndpointState.Unresponsive, endpointRemovedArgs.State);
            Assert.Equal(endpoint, endpointRemovedArgs.Endpoint);
        }
    }
}
