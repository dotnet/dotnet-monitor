// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    internal sealed class CallbackActionFactory : ICollectionRuleActionFactory<BaseRecordOptions>
    {
        private readonly CallbackActionService _service;

        public CallbackActionFactory(CallbackActionService service)
        {
            _service = service;
        }

        public ICollectionRuleAction Create(IProcessInfo processInfo, BaseRecordOptions options)
        {
            return new CallbackAction(_service);
        }
    }

    internal sealed class CallbackAction : ICollectionRuleAction
    {
        public const string ActionName = nameof(CallbackAction);

        private readonly CallbackActionService _service;
        private readonly TaskCompletionSource _startCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Started => _startCompletionSource.Task;

        public CallbackAction(CallbackActionService service)
        {
            _service = service;
        }

        public Task StartAsync(CollectionRuleMetadata collectionRuleMetadata, CancellationToken token)
        {
            return StartAsync(token); // We don't care about collectionRuleMetadata for testing (yet)
        }

        public Task StartAsync(CancellationToken token)
        {
            _startCompletionSource.TrySetResult();
            return _service.NotifyListeners(token);
        }

        public Task<CollectionRuleActionResult> WaitForCompletionAsync(CancellationToken token)
        {
            return Task.FromResult(new CollectionRuleActionResult());
        }
    }

    internal sealed class DelayedCallbackActionFactory : ICollectionRuleActionFactory<BaseRecordOptions>
    {
        private readonly CallbackActionService _service;

        public DelayedCallbackActionFactory(CallbackActionService service)
        {
            _service = service;
        }

        public ICollectionRuleAction Create(IProcessInfo processInfo, BaseRecordOptions options)
        {
            return new DelayedCallbackAction(_service);
        }
    }

    internal sealed class DelayedCallbackAction : ICollectionRuleAction
    {
        public const string ActionName = nameof(DelayedCallbackAction);

        private readonly CallbackActionService _service;
        private readonly TaskCompletionSource _startCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Started => _startCompletionSource.Task;

        public DelayedCallbackAction(CallbackActionService service)
        {
            _service = service;
        }

        public Task StartAsync(CollectionRuleMetadata collectionRuleMetadata, CancellationToken token)
        {
            return StartAsync(token); // We don't care about collectionRuleMetadata for testing (yet)
        }

        public Task StartAsync(CancellationToken token)
        {
            _startCompletionSource.TrySetResult();
            return _service.NotifyListeners(token);
        }

        public Task<CollectionRuleActionResult> WaitForCompletionAsync(CancellationToken token)
        {
            var currentTime = _service.TimeProvider.GetUtcNow();
            while (_service.TimeProvider.GetUtcNow() == currentTime)
            {
                // waiting for clock to be ticked (simulated time)
                token.ThrowIfCancellationRequested();
            }

            return Task.FromResult(new CollectionRuleActionResult());
        }
    }

    internal sealed class CallbackActionService
    {
        public TimeProvider TimeProvider { get; }
        private readonly List<CompletionEntry> _entries = new();
        private readonly SemaphoreSlim _entriesSemaphore = new(1);
        private readonly List<DateTime> _executionTimestamps = new();
        private readonly ITestOutputHelper _outputHelper;

        private int _nextId = 1;

        public CallbackActionService(ITestOutputHelper outputHelper, TimeProvider timeProvider = null)
        {
            _outputHelper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));
            TimeProvider = timeProvider ?? TimeProvider.System;
        }

        public async Task NotifyListeners(CancellationToken token)
        {
            await _entriesSemaphore.WaitAsync(token);
            try
            {
                lock (_executionTimestamps)
                {
                    _executionTimestamps.Add(TimeProvider.GetUtcNow().UtcDateTime);
                }

                _outputHelper.WriteLine("[Callback] Completing {0} source(s).", _entries.Count);

                foreach (var entry in _entries)
                {
                    entry.Complete();
                }
                _entries.Clear();
            }
            finally
            {
                _entriesSemaphore.Release();
            }
        }

        /// <summary>
        /// Registers a callback with the Callback action that will complete when
        /// the Callback action is invoked.
        /// </summary>
        /// <returns>
        /// A <see cref="Task{Task}"/> that completes when the callback has finished
        /// being registered. The inner <see cref="Task"/> will complete when the callback
        /// is invoked.
        /// </returns>
        /// <remarks>
        /// Await this method to wait for the callback to be registered; await the inner
        /// task to wait for the callback to be invoked. The <paramref name="token"/> parameter
        /// only cancels registration if the registration has not completed; it does not cancel
        /// the inner task that represents the callback invocation.
        /// </remarks>
        public async Task<Task> StartWaitForCallbackAsync(CancellationToken token)
        {
            int id = _nextId++;
            string name = $"Callback{id}";

            CompletionEntry entry = new(_outputHelper, name);

            await _entriesSemaphore.WaitAsync(token);
            try
            {
                _outputHelper.WriteLine("[Test] Registering {0}.", name);

                _entries.Add(entry);
            }
            finally
            {
                _entriesSemaphore.Release();
            }

            return entry.Task;
        }

        public IReadOnlyCollection<DateTime> ExecutionTimestamps
        {
            get
            {
                lock (_executionTimestamps)
                {
                    return _executionTimestamps.AsReadOnly();
                }
            }
        }

        private sealed class CompletionEntry
        {
            private readonly TaskCompletionSource<object> _completionSource =
                new(TaskCreationOptions.RunContinuationsAsynchronously);
            private readonly string _name;
            private readonly ITestOutputHelper _outputHelper;

            public CompletionEntry(ITestOutputHelper outputHelper, string name)
            {
                _name = name;
                _outputHelper = outputHelper;
            }

            public void Complete()
            {
                _outputHelper.WriteLine("[Callback] Begin completing {0}.", _name);
                if (!_completionSource.TrySetResult(null))
                {
                    _outputHelper.WriteLine("[Callback] Unable to complete {0}.", _name);
                }
                _outputHelper.WriteLine("[Callback] End completing {0}.", _name);
            }

            public Task Task => _completionSource.Task;
        }
    }
}
