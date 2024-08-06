// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal interface IEgressOperationStore
    {
        Task<ExecutionResult<EgressResult>> ExecuteOperation(IEgressOperation egressOperation);

        Task<Guid> AddOperation(IEgressOperation egressOperation, string limitKey);

        void StopOperation(Guid operationId, Action<Exception> onStopException);

        void MarkOperationAsRunning(Guid operationId);

        void CancelOperation(Guid operationId);

        void CompleteOperation(Guid operationId, ExecutionResult<EgressResult> result);

        IEnumerable<Models.OperationSummary> GetOperations(ProcessKey? processKey, string? tags);

        Models.OperationStatus GetOperationStatus(Guid operationId);
    }
}
