// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal sealed class InProcessEgressOperation : IEgressOperation
    {
        private readonly KeyValueLogScope _scope;

        public EgressProcessInfo ProcessInfo { get; private set; }
        public string EgressProviderName { get { return null; } }
        public bool IsStoppable { get { return _operation?.IsStoppable ?? false; } }
        public ISet<string> Tags { get; private set; }

        private readonly IInProcessOperation _operation;

        public InProcessEgressOperation(IProcessInfo processInfo, KeyValueLogScope scope, string tags, IInProcessOperation operation)
        {
            _scope = scope;
            _operation = operation;
            Tags = Utilities.SplitTags(tags);

            ProcessInfo = new EgressProcessInfo(processInfo.ProcessName, processInfo.EndpointInfo.ProcessId, processInfo.EndpointInfo.RuntimeInstanceCookie);
        }

        public async Task<ExecutionResult<EgressResult>> ExecuteAsync(IServiceProvider serviceProvider, CancellationToken token)
        {
            ILogger<InProcessEgressOperation> logger = CreateLogger(serviceProvider);

            using var _ = logger.BeginScope(_scope);

            return await ExecutionHelper.InvokeAsync(async (token) =>
            {
                await _operation.ExecuteAsync(null, token);

                logger.GeneratedInProcessArtifact();

                return ExecutionResult<EgressResult>.Succeeded(new EgressResult());
            }, logger, token);
        }

        public void Validate(IServiceProvider serviceProvider)
        {
        }

        public static Task ValidateAsync(IServiceProvider serviceProvider, string endpointName, CancellationToken token)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken token)
        {
            return _operation.StopAsync(token);
        }

        private static ILogger<InProcessEgressOperation> CreateLogger(IServiceProvider serviceProvider)
        {
            return serviceProvider
             .GetRequiredService<ILoggerFactory>()
             .CreateLogger<InProcessEgressOperation>();
        }
    }
}
