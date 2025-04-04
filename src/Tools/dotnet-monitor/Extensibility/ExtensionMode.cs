// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.Diagnostics.Tools.Monitor.Extensibility
{
    [JsonConverter(typeof(JsonStringEnumConverter<ExtensionMode>))]
    internal enum ExtensionMode
    {
        Execute,
        Validate
    }
}
