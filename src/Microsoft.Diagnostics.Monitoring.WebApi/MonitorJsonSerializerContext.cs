// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Diagnostics.Monitoring.Options;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    [JsonSerializable(typeof(CaptureParametersConfiguration))]
    [JsonSerializable(typeof(CollectionRuleDetailedDescription))]
    [JsonSerializable(typeof(Dictionary<string, CollectionRuleDescription>))]
    [JsonSerializable(typeof(Dictionary<string, string>))]
    [JsonSerializable(typeof(DotnetMonitorInfo))]
    [JsonSerializable(typeof(DumpType))]
    [JsonSerializable(typeof(EventMetricsConfiguration))]
    [JsonSerializable(typeof(EventPipeConfiguration))]
    [JsonSerializable(typeof(ExceptionsConfiguration))]
    [JsonSerializable(typeof(FileResult))]
    [JsonSerializable(typeof(Guid?))]
    [JsonSerializable(typeof(IEnumerable<OperationSummary>))]
    [JsonSerializable(typeof(IEnumerable<ProcessIdentifier>))]
    [JsonSerializable(typeof(IList<ProcessIdentifier>))]
    [JsonSerializable(typeof(LogsConfiguration))]
    [JsonSerializable(typeof(MetricsOptions))]
    [JsonSerializable(typeof(OperationStatus))]
    [JsonSerializable(typeof(ProcessInfo))]
    [JsonSerializable(typeof(ProblemDetails))]
    [JsonSerializable(typeof(TraceProfile))]
    [JsonSerializable(typeof(ValidationProblemDetails))]
    partial class MonitorJsonSerializerContext : JsonSerializerContext
    {
    }
}
