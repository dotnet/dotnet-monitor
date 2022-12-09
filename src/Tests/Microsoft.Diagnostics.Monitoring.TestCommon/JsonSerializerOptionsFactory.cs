// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    public static class JsonSerializerOptionsFactory
    {
        public static JsonSerializerOptions Create(JsonIgnoreCondition ignoreCondition)
        {
            JsonSerializerOptions serializerOptions = new()
            {
#if NET5_0_OR_GREATER
                DefaultIgnoreCondition = (System.Text.Json.Serialization.JsonIgnoreCondition)ignoreCondition,
#else
                IgnoreNullValues = ignoreCondition == JsonIgnoreCondition.WhenWritingNull,
#endif
            };
            return serializerOptions;
        }


        /// <summary>
        /// Controls how the System.Text.Json.Serialization.JsonIgnoreAttribute ignores properties
        /// on serialization and deserialization.
        /// </summary>
        /// <remarks>This is a copy of System.Text.Json.Serialization.JsonIgnoreCondition for use older versions of .net.</remarks>
        public enum JsonIgnoreCondition
        {
            //
            // Summary:
            //     Property will always be serialized and deserialized, regardless of System.Text.Json.JsonSerializerOptions.IgnoreNullValues
            //     configuration.
            Never = 0,
            //
            // Summary:
            //     Property will always be ignored.
            Always = 1,
            //
            // Summary:
            //     Property will only be ignored if it is null.
            WhenWritingDefault = 2,
            //
            // Summary:
            //     If the value is null, the property is ignored during serialization. This is applied
            //     only to reference-type properties and fields.
            WhenWritingNull = 3
        }
    }
}
