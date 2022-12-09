// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    public static class JsonSerializerOptionsFactory
    {
        public static JsonSerializerOptions Create(JsonIgnoreCondition ignoreCondition)
        {
            JsonSerializerOptions serializerOptions = new()
            {
                DefaultIgnoreCondition = ignoreCondition,
            };
            return serializerOptions;
        }
    }
}
