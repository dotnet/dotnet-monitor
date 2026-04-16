// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using NJsonSchema;
using NJsonSchema.Generation;
using System;
using System.Linq;
using System.Reflection;

namespace Microsoft.Diagnostics.Monitoring.ConfigurationSchema
{
    /// <summary>
    /// Workaround for NJsonSchema 11.6.0+ where [EnumDataType] support (PR #1890 in https://github.com/RicoSuter/NJsonSchema/pull/1890)
    /// causes nullable enum properties to lose their null type in oneOf schemas.
    /// When a property is Nullable&lt;TEnum&gt; with [EnumDataType(typeof(TEnum))],
    /// NJsonSchema replaces the effective type with TEnum (non-nullable), dropping
    /// the null branch from oneOf.
    /// </summary>
    internal sealed class NullableEnumSchemaProcessor : ISchemaProcessor
    {
        public void Process(SchemaProcessorContext context)
        {
            foreach (PropertyInfo property in context.ContextualType.Type.GetProperties())
            {
                Type propertyType = property.PropertyType;

                // Check if the property is Nullable<TEnum> where TEnum is an enum
                if (!propertyType.IsGenericType ||
                    propertyType.GetGenericTypeDefinition() != typeof(Nullable<>) ||
                    !propertyType.GetGenericArguments()[0].IsEnum)
                {
                    continue;
                }

                if (!context.Schema.Properties.TryGetValue(property.Name, out JsonSchemaProperty? schemaProperty))
                {
                    continue;
                }

                // If the property has a oneOf with enum ref(s) but no null type, add one
                if (schemaProperty.OneOf.Count > 0 &&
                    !schemaProperty.OneOf.Any(s => s.Type.HasFlag(JsonObjectType.Null)))
                {
                    JsonSchema nullSchema = new JsonSchema();
                    nullSchema.Type = JsonObjectType.Null;

                    // Rebuild OneOf with null type first, then existing entries
                    JsonSchema[] existing = schemaProperty.OneOf.ToArray();
                    schemaProperty.OneOf.Clear();
                    schemaProperty.OneOf.Add(nullSchema);
                    foreach (JsonSchema entry in existing)
                    {
                        schemaProperty.OneOf.Add(entry);
                    }
                }
            }
        }
    }
}
