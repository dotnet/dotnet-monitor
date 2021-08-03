using Microsoft.Diagnostics.Monitoring.Tool.UnitTests;
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

            var tempVar = JsonSchema.FromType<JsonConsoleFormatterOptions>();
            schema.Definitions.Add(nameof(JsonConsoleFormatterOptions), tempVar);

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

            JsonSchema tempSchema = new JsonSchema();

            //tempSchema.Type = object;

            JsonSchemaProperty formatterNameConstraintProperty = new JsonSchemaProperty();
            JsonSchemaProperty formatterOptionsConstraintProperty = new JsonSchemaProperty();

            JsonSchema tempSchema2 = new JsonSchema();

            tempSchema2.Reference = tempVar;

            formatterOptionsConstraintProperty.OneOf.Add(tempSchema2);

            formatterNameConstraintProperty.ExtensionData = new Dictionary<string, object>();
            formatterNameConstraintProperty.ExtensionData.Add("const", "Json");

            tempSchema.Properties.Add(nameof(ConsoleLoggerOptions.FormatterName), formatterNameConstraintProperty);

            tempSchema.Properties.Add(nameof(ConsoleLoggerOptions.FormatterOptions), formatterOptionsConstraintProperty);

            tempSchema.RequiredProperties.Add(nameof(ConsoleLoggerOptions.FormatterName));

            schema.Definitions[nameof(ConsoleLoggerOptions)].OneOf.Add(tempSchema);

            //schema.Definitions[nameof(ConsoleLoggerOptions)].OneOf.Add(tempSchemaSimple);
            //schema.Definitions[nameof(ConsoleLoggerOptions)].OneOf.Add(tempSchemaSchemad);

            string schemaPayload = schema.ToJson();

            //Normalize newlines embedded into json
            schemaPayload = schemaPayload.Replace(@"\r\n", @"\n", StringComparison.Ordinal);
            return schemaPayload;
        }
    }
}
