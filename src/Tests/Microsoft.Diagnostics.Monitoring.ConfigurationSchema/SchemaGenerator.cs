﻿using Microsoft.Diagnostics.Tools.Monitor;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NJsonSchema;
using NJsonSchema.Generation;
using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Monitoring.ConfigurationSchema
{
    internal sealed class SchemaGenerator
    {
        public string GenerateSchema()
        {
            var settings = new JsonSchemaGeneratorSettings();
            settings.SerializerSettings = new JsonSerializerSettings();
            settings.SerializerSettings.Converters.Add(new StringEnumConverter());
            JsonSchema schema = JsonSchema.FromType<RootOptions>(settings);
            schema.Id = @"https://www.github.com/dotnet/dotnet-monitor";
            schema.Title = "DotnetMonitorConfiguration";

            //Allow other properties in the schema.
            schema.AdditionalPropertiesSchema = JsonSchema.CreateAnySchema();

            //TODO Figure out a better way to add object defaults
            schema.Definitions[nameof(EgressOptions)].Properties[nameof(EgressOptions.AzureBlobStorage)].Default = JsonSchema.CreateAnySchema();
            schema.Definitions[nameof(EgressOptions)].Properties[nameof(EgressOptions.FileSystem)].Default = JsonSchema.CreateAnySchema();
            schema.Definitions[nameof(EgressOptions)].Properties[nameof(EgressOptions.Properties)].Default = JsonSchema.CreateAnySchema();

            //Make the default for each property and empty object.
            foreach (KeyValuePair<string, JsonSchemaProperty> kvp in schema.Properties)
            {
                kvp.Value.Default = JsonSchema.CreateAnySchema();
            }

            string schemaPayload = schema.ToJson();

            //Normalize newlines embedded into json
            schemaPayload = schemaPayload.Replace(@"\r\n", @"\n", StringComparison.Ordinal);
            return schemaPayload;
        }
    }
}
