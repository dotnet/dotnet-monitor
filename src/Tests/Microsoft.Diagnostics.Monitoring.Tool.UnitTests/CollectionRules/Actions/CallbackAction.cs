// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests.CollectionRules.Actions
{
    internal sealed class CallbackAction : ICollectionRuleAction<object>
    {
        public static readonly string ActionName = nameof(CallbackAction);

        private readonly CallbackActionCallbackService _service;

        public CallbackAction(CallbackActionCallbackService service)
        {
            _service = service;
        }

        public async Task<CollectionRuleActionResult> ExecuteAsync(object options, IEndpointInfo endpointInfo, CancellationToken token)
        {
            await _service.NotifyListeners(token);

            return new CollectionRuleActionResult();
        }
    }

    internal sealed class CallbackActionCallbackService
    {
        private readonly List<CompletionEntry> _entries = new();
        private readonly SemaphoreSlim _entriesSemaphore = new(1);
        private readonly List<DateTime> _executionTimestamps = new();
        private readonly ITestOutputHelper _outputHelper;

        private int _nextId = 1;

        public CallbackActionCallbackService(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));
        }

        public async Task NotifyListeners(CancellationToken token)
        {
            await _entriesSemaphore.WaitAsync(token);
            try
            {
                lock (_executionTimestamps)
                {
                    _executionTimestamps.Add(DateTime.Now);
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

        public async Task WaitWithCancellationAsync(CancellationToken token)
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

            await entry.WithCancellation(token);
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

            public async Task WithCancellation(CancellationToken token)
            {
                _outputHelper.WriteLine("[Test] Begin waiting for {0} completion.", _name);
                await _completionSource.WithCancellation(token);
                _outputHelper.WriteLine("[Test] End waiting for {0} completion.", _name);
            }
        }
    }
}
