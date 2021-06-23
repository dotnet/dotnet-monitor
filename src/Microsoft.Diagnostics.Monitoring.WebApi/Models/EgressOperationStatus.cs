// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Models
{
    /// <summary>
    /// Represents the state of a long running operation. Used for all types of results, including
    /// successes and failures.
    /// </summary>
    public class OperationStatus
    {
        //CONSIDER Should we also have a retry-after? Not sure we can produce meaningful values for this.
        public OperationState Status { get; set; }

        public Guid OperationId { get; set; }

        public DateTime CreatedDateTime { get; set; }

        //Success cases
        public string ResourceLocation { get; set; }

        //Failure cases
        public OperationError Error { get; set; }
    }

    public enum OperationState
    {
        Running,
        Succeeded,
        Failed,
        Cancelled
    }

    public class OperationError
    {
        public string Code { get; set; }
        public string Message { get; set; }
    }
}
