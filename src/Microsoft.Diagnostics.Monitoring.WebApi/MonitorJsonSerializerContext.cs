// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Diagnostics.Monitoring.Options;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    [JsonSerializable(typeof(CaptureParametersConfiguration))]
    [JsonSerializable(typeof(EventMetricsConfiguration))]
    [JsonSerializable(typeof(EventPipeConfiguration))]
    [JsonSerializable(typeof(ExceptionsConfiguration))]
    [JsonSerializable(typeof(LogsConfiguration))]
    [JsonSerializable(typeof(IList<ProcessIdentifier>))]
    partial class MonitorJsonSerializerContext : JsonSerializerContext
    {
    }
}
