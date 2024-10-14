﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Models
{
    public class ExceptionInstance
    {
        [JsonPropertyName("id")]
        public ulong Id { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("typeName")]
        public string TypeName { get; set; } = string.Empty;

        [JsonPropertyName("moduleName")]
        public string ModuleName { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("activity")]
        public Activity? Activity { get; set; }

        [JsonPropertyName("innerExceptions")]
        public InnerExceptionId[] InnerExceptionIds { get; set; } = [];

        [JsonPropertyName("stack")]
        public CallStack? CallStack { get; set; }
    }

    public class Activity
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("idFormat")]
        public ActivityIdFormat IdFormat { get; set; } = ActivityIdFormat.Unknown;
    }

    public class InnerExceptionId
    {
        public static implicit operator InnerExceptionId(ulong id)
            => new InnerExceptionId()
            {
                Id = id,
            };

        [JsonPropertyName("id")]
        public ulong Id { get; set; }
    }
}
