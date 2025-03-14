// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    [JsonConverter(typeof(JsonStringEnumConverter<DiagnosticPortConnectionMode>))]
    public enum DiagnosticPortConnectionMode
    {
        Connect,
        Listen
    }
}
