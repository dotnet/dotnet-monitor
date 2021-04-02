using Microsoft.Diagnostics.Monitoring.UnitTests.Options;
using NJsonSchema;
using NJsonSchema.Generation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Diagnostics.Monitoring.ConfigurationSchema
{
    internal sealed class SchemaGenerator
    {
        public string GenerateSchema()
        {
            var settings = new JsonSchemaGeneratorSettings();
            JsonSchema schema = JsonSchema.FromType<RootOptions>(settings);
            schema.Id = @"https://www.github.com/dotnet/dotnet-monitor";
            schema.Title = "DotnetMonitorConfiguration";

            //Allow other properties in the schema.
            schema.AdditionalPropertiesSchema = JsonSchema.CreateAnySchema();

            //HACK Even though Properties is defined as JsonDataExtension, it still emits a property
            //called 'Properties' into the schema, instead of just specifying additionalProperties.
            //There is likely a more elegant way to fix this.
            schema.Definitions[nameof(EgressProvider)].Properties.Remove(nameof(EgressProvider.Properties));

            //TODO Figure out a better way to add object defaults
            schema.Definitions[nameof(EgressOptions)].Properties[nameof(EgressOptions.Providers)].Default = JsonSchema.CreateAnySchema();
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
