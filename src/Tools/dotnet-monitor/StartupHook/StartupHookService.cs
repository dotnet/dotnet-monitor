// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.StartupHook
{
    internal sealed class StartupHookService :
        IDiagnosticLifetimeService
    {
        // Intent is to ship a single TFM of the startup hook, which should be the lowest supported version.
        // If necessary, the startup hook should dynamically access APIs for higher version TFMs and handle
        // all exceptions appropriately.
        private const string Tfm = "net6.0";
        private const string FileName = "Microsoft.Diagnostics.Monitoring.StartupHook.dll";

        private readonly IInProcessFeatures _inProcessFeatures;
        private readonly StartupHookApplicator _startupHookApplicator;
        private readonly TaskCompletionSource<bool> _appliedSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<bool> Applied => _appliedSource.Task;

        public StartupHookService(
            IInProcessFeatures inProcessFeatures,
            StartupHookApplicator startupHookApplicator)
        {
            _inProcessFeatures = inProcessFeatures;
            _startupHookApplicator = startupHookApplicator;
        }

        public async ValueTask StartAsync(CancellationToken cancellationToken)
        {
            if (!_inProcessFeatures.IsStartupHookRequired)
            {
                _appliedSource.TrySetResult(false);
                return;
            }

            _appliedSource.TrySetResult(await _startupHookApplicator.ApplyAsync(Tfm, FileName, cancellationToken));
        }

        public ValueTask StopAsync(CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }
    }
}
