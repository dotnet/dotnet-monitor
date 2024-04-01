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
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("module")]
        public string TypeModuleName { get; set; }

        [JsonPropertyName("value")]
        public string Value { get; set; }

        [JsonPropertyName("isIn")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool IsInParameter { get; set; }

        [JsonPropertyName("isOut")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool IsOutParameter { get; set; }

        [JsonPropertyName("isByRef")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool IsByRefParameter { get; set; }
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

        [JsonPropertyName("dateTime")]
        public DateTime CapturedDateTime { get; set; }

        [JsonPropertyName("module")]
        public string ModuleName { get; set; }

        [JsonPropertyName("type")]
        public string TypeName { get; set; }

        [JsonPropertyName("method")]
        public string MethodName { get; set; }

        [JsonPropertyName("parameters")]
        public IList<CapturedParameter> Parameters { get; set; } = [];
    }

    public class CapturedParametersResult
    {
        [JsonPropertyName("captures")]
        public IList<CapturedMethod> CapturedMethods { get; set; } = [];
    }
}
