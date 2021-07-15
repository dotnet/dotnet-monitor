﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

#if UNITTEST
namespace Microsoft.Diagnostics.Monitoring.UnitTests.Models
#else
namespace Microsoft.Diagnostics.Monitoring.WebApi.Models
#endif
{
    /// <summary>
    /// Represents a partial model when enumerating all operations.
    /// </summary>
    public class OperationSummary
    {
        public Guid OperationId { get; set; }

        public DateTime CreatedDateTime { get; set; }

        public OperationState Status { get; set; }
    }

    /// <summary>
    /// Represents the state of a long running operation. Used for all types of results, including successes and failures.
    /// </summary>
    public class OperationStatus : OperationSummary
    {
        //CONSIDER Should we also have a retry-after? Not sure we can produce meaningful values for this.

        //Success cases
        public string ResourceLocation { get; set; }

        //Failure cases
        public OperationError Error { get; set; }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
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
