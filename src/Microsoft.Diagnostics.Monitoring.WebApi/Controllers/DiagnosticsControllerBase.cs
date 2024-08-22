// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Controllers
{
    public abstract class DiagnosticsControllerBase : ControllerBase
    {
        protected DiagnosticsControllerBase(IServiceProvider serviceProvider, ILogger logger) :
            this(serviceProvider.GetRequiredService<IDiagnosticServices>(), serviceProvider.GetRequiredService<IEgressOperationStore>(), logger)
        { }

        private protected DiagnosticsControllerBase(IDiagnosticServices diagnosticServices, IEgressOperationStore operationStore, ILogger logger)
        {
            DiagnosticServices = diagnosticServices ?? throw new ArgumentNullException(nameof(diagnosticServices));
            OperationStore = operationStore ?? throw new ArgumentNullException(nameof(operationStore));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected Task<ActionResult> InvokeForProcess(Func<IProcessInfo, ActionResult> func, ProcessKey? processKey, string? artifactType = null)
        {
            Func<IProcessInfo, Task<ActionResult>> asyncFunc =
                processInfo => Task.FromResult(func(processInfo));

            return InvokeForProcess(asyncFunc, processKey, artifactType);
        }

        protected async Task<ActionResult> InvokeForProcess(Func<IProcessInfo, Task<ActionResult>> func, ProcessKey? processKey, string? artifactType)
        {
            ActionResult<object> result = await InvokeForProcess<object>(async processInfo => await func(processInfo), processKey, artifactType);

            return result.Result!;
        }

        protected Task<ActionResult<T>> InvokeForProcess<T>(Func<IProcessInfo, ActionResult<T>> func, ProcessKey? processKey, string? artifactType = null)
        {
            return InvokeForProcess(processInfo => Task.FromResult(func(processInfo)), processKey, artifactType);
        }

        protected async Task<ActionResult<T>> InvokeForProcess<T>(Func<IProcessInfo, Task<ActionResult<T>>> func, ProcessKey? processKey, string? artifactType = null)
        {
            IDisposable? artifactTypeRegistration = null;
            if (!string.IsNullOrEmpty(artifactType))
            {
                KeyValueLogScope artifactTypeScope = new KeyValueLogScope();
                artifactTypeScope.AddArtifactType(artifactType);
                artifactTypeRegistration = Logger.BeginScope(artifactTypeScope);
            }

            try
            {
                return await this.InvokeService(async () =>
                {
                    IProcessInfo processInfo = await DiagnosticServices.GetProcessAsync(processKey, HttpContext.RequestAborted);

                    KeyValueLogScope processInfoScope = new KeyValueLogScope();
                    processInfoScope.AddArtifactEndpointInfo(processInfo.EndpointInfo);
                    using var _ = Logger.BeginScope(processInfoScope);

                    Logger.ResolvedTargetProcess();

                    return await func(processInfo);
                }, Logger);
            }
            finally
            {
                artifactTypeRegistration?.Dispose();
            }
        }

        protected async Task<ActionResult> Result(
            string artifactType,
            string? providerName,
            IArtifactOperation operation,
            IProcessInfo processInfo,
            string? tags,
            bool asAttachment = true)
        {
            KeyValueLogScope scope = Utilities.CreateArtifactScope(artifactType, processInfo.EndpointInfo);

            if (string.IsNullOrEmpty(providerName))
            {
                await RegisterCurrentHttpResponseAsOperation(processInfo, artifactType, tags, operation);
                return new OutputStreamResult(
                    operation,
                    asAttachment ? operation.GenerateFileName() : null,
                    scope);
            }
            else
            {
                return await SendToEgress(new EgressOperation(
                    operation,
                    providerName,
                    default, // Use default artifact name
                    processInfo,
                    scope,
                    tags),
                    limitKey: artifactType);
            }
        }

        private async Task RegisterCurrentHttpResponseAsOperation(IProcessInfo processInfo, string artifactType, string? tags, IArtifactOperation operation)
        {
            // While not strictly a Location redirect, use the same header as externally egressed operations for consistency.
            HttpContext.Response.Headers["Location"] = await RegisterOperation(
                new HttpResponseEgressOperation(HttpContext, processInfo, tags, operation),
                limitKey: artifactType);
        }

        private async Task<string?> RegisterOperation(IEgressOperation egressOperation, string limitKey)
        {
            // Will throw TooManyRequestsException if there are too many concurrent operations.
            Guid operationId = await OperationStore.AddOperation(egressOperation, limitKey);
            return this.Url.Action(
                action: nameof(OperationsController.GetOperationStatus),
                controller: OperationsController.ControllerName, new { operationId = operationId },
                protocol: this.HttpContext.Request.Scheme, this.HttpContext.Request.Host.ToString());
        }

        private async Task<ActionResult> SendToEgress(IEgressOperation egressOperation, string limitKey)
        {
            string? operationUrl = await RegisterOperation(egressOperation, limitKey);
            return Accepted(operationUrl);
        }

        private protected IDiagnosticServices DiagnosticServices { get; }

        private protected IEgressOperationStore OperationStore { get; }

        protected ILogger Logger { get; }
    }
}
