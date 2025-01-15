﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal class EgressOperation : IEgressOperation
    {
        private readonly Func<IEgressService, CancellationToken, Task<EgressResult>> _egress;
        private readonly KeyValueLogScope _scope;
        public EgressProcessInfo ProcessInfo { get; private set; }
        public string EgressProviderName { get; private set; }
        public bool IsStoppable { get { return _operation?.IsStoppable ?? false; } }
        public ISet<string> Tags { get; private set; }

        public Task Started => _operation.Started;

        private readonly IArtifactOperation _operation;

        public EgressOperation(IArtifactOperation operation, string endpointName, string? artifactName, IProcessInfo processInfo, KeyValueLogScope scope, string? tags, CollectionRuleMetadata? collectionRuleMetadata = null)
        {
            _egress = (service, token) => service.EgressAsync(
                endpointName,
                operation.ExecuteAsync,
                artifactName ?? operation.GenerateFileName(),
                operation.ContentType,
                processInfo.EndpointInfo,
                collectionRuleMetadata,
                token);

            EgressProviderName = endpointName;
            _scope = scope;

            ProcessInfo = new EgressProcessInfo(processInfo.ProcessName, processInfo.EndpointInfo.ProcessId, processInfo.EndpointInfo.RuntimeInstanceCookie);
            Tags = Utilities.SplitTags(tags);
            _operation = operation;
        }

        public async Task<ExecutionResult<EgressResult>> ExecuteAsync(IServiceProvider serviceProvider, CancellationToken token)
        {
            ILogger<EgressOperation> logger = CreateLogger(serviceProvider);

            using var _ = logger.BeginScope(_scope);

            return await ExecutionHelper.InvokeAsync(async (token) =>
            {
                IEgressService egressService = serviceProvider
                    .GetRequiredService<IEgressService>();

                EgressResult egressResult = await _egress(egressService, token);

                logger.EgressedArtifact(egressResult.Value);

                // The remaining code is creating a JSON object with a single property and scalar value
                // that indicates where the stream data was egressed. Because the name of the artifact is
                // automatically generated by the REST API and the caller of the endpoint might not know
                // the specific configuration information for the egress provider, this value allows the
                // caller to more easily find the artifact after egress has completed.
                return ExecutionResult<EgressResult>.Succeeded(egressResult);
            }, logger, token);
        }

        public void Validate(IServiceProvider serviceProvider)
        {
            serviceProvider
                .GetRequiredService<IEgressService>()
                .ValidateProviderExists(EgressProviderName);
        }

        public static async Task ValidateAsync(IServiceProvider serviceProvider, string endpointName, CancellationToken token)
        {
            ILogger<EgressOperation> logger = CreateLogger(serviceProvider);

            await ExecutionHelper.InvokeAsync(async (token) =>
            {
                IEgressService egressService = serviceProvider
                    .GetRequiredService<IEgressService>();

                await egressService.ValidateProviderOptionsAsync(endpointName, token);

                return ExecutionResult<EgressResult>.Succeeded(new EgressResult());
            }, logger, token);
        }

        public Task StopAsync(CancellationToken token)
        {
            if (_operation == null)
            {
                throw new InvalidOperationException();
            }

            return _operation.StopAsync(token);
        }

        private static ILogger<EgressOperation> CreateLogger(IServiceProvider serviceProvider)
        {
            return serviceProvider
             .GetRequiredService<ILoggerFactory>()
             .CreateLogger<EgressOperation>();
        }
    }
}
