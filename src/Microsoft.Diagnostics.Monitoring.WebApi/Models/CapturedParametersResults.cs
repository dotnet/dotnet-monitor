// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;

#nullable enable

namespace Microsoft.Diagnostics.Monitoring.WebApi.Models
{
    public sealed class CapturedParameter
    {
        [JsonPropertyName("parameterName")]
        public required string Name { get; init; }

        [JsonPropertyName("typeName")]
        public required string Type { get; init; }

        [JsonPropertyName("moduleName")]
        public required string TypeModuleName { get; init; }

        [JsonPropertyName("value")]
        public string? Value { get; init; }

        [JsonPropertyName("evalFailReason")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public EvaluationFailureReason EvalFailReason { get; init; }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum EvaluationFailureReason
    {
        None = 0,
        NotSupported = 1,
        HasSideEffects = 2,
        Unknown = 3
    }

    public sealed class CapturedMethod
    {
        [JsonPropertyName("activityId")]
        public string? ActivityId { get; init; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        [JsonPropertyName("activityIdFormat")]
        public ActivityIdFormat ActivityIdFormat { get; init; }

        [JsonPropertyName("threadId")]
        public int ThreadId { get; init; }

        [JsonPropertyName("timestamp")]
        public DateTime CapturedDateTime { get; init; }

        [JsonPropertyName("moduleName")]
        public required string ModuleName { get; init; }

        [JsonPropertyName("typeName")]
        public required string TypeName { get; init; }

        [JsonPropertyName("methodName")]
        public required string MethodName { get; init; }

        [JsonPropertyName("parameters")]
        public IList<CapturedParameter> Parameters { get; init; } = [];
    }
}
