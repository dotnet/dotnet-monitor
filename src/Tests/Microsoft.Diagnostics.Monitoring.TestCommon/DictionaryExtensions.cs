// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    public static class DictionaryExtensions
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };

        public static bool TryGetValue<T>(this IDictionary<string, JsonElement> dictionary, string key, out T value)
        {
            if (dictionary.TryGetValue(key, out JsonElement element))
            {
                try
                {
                    value = JsonSerializer.Deserialize<T>(element.GetRawText(), SerializerOptions);
                    return true;
                }
                catch
                {
                }
            }

            value = default(T);
            return false;
        }
    }
}
