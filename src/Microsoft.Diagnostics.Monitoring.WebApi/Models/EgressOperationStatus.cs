﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json.Serialization;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Models
{
    /// <summary>
    /// Represents a partial model when enumerating all operations.
    /// </summary>
    public class OperationSummary
    {
        [JsonPropertyName("operationId")]
        public Guid OperationId { get; set; }

        [JsonPropertyName("createdDateTime")]
        public DateTime CreatedDateTime { get; set; }

        [JsonPropertyName("status")]
        public OperationState Status { get; set; }

        [JsonPropertyName("process")]
        public OperationProcessInfo Process { get; set; }
    }

    /// <summary>
    /// Represents the details of a given process used in an operation.
    /// </summary>
    public class OperationProcessInfo
    {
        [JsonPropertyName("pid")]
        public int ProcessId { get; set; }

        [JsonPropertyName("uid")]
        public Guid Uid { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    /// <summary>
    /// Represents the state of a long running operation. Used for all types of results, including successes and failures.
    /// </summary>
    public class OperationStatus : OperationSummary
    {
        //CONSIDER Should we also have a retry-after? Not sure we can produce meaningful values for this.

        //Success cases
        [JsonPropertyName("resourceLocation")]
        public string ResourceLocation { get; set; }

        //Failure cases
        [JsonPropertyName("error")]
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
        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }
}
