// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Models
{
    public class CapturedParameter
    {
        [JsonPropertyName("parameterName")]
        public string Name { get; set; }

        [JsonPropertyName("typeName")]
        public string Type { get; set; }

        [JsonPropertyName("moduleName")]
        public string TypeModuleName { get; set; }

        [JsonPropertyName("value")]
        public string Value { get; set; }

        [JsonPropertyName("evalFailReason")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public EvaluationFailureReason EvalFailReason { get; set; }

        [JsonPropertyName("isNull")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool IsNull { get; set; }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum EvaluationFailureReason
    {
        None = 0,
        NotSupported = 1,
        HasSideEffects = 2,
        Unknown = 3
    }

    public class CapturedMethod
    {
        [JsonPropertyName("activityId")]
        public string ActivityId { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        [JsonPropertyName("activityIdFormat")]
        public ActivityIdFormat ActivityIdFormat { get; set; }

        [JsonPropertyName("threadId")]
        public int ThreadId { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime CapturedDateTime { get; set; }

        [JsonPropertyName("moduleName")]
        public string ModuleName { get; set; }

        [JsonPropertyName("typeName")]
        public string TypeName { get; set; }

        [JsonPropertyName("methodName")]
        public string MethodName { get; set; }

        [JsonPropertyName("parameters")]
        public IList<CapturedParameter> Parameters { get; set; } = [];
    }
}
