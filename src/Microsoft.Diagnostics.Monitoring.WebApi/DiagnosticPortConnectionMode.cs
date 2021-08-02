// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

#if UNITTEST
namespace Microsoft.Diagnostics.Monitoring.TestCommon.Options
#else
namespace Microsoft.Diagnostics.Monitoring.WebApi
#endif
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DiagnosticPortConnectionMode
    {
        Connect,
        Listen
    }
}
