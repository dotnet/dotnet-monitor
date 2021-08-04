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

            JsonSchema jsonConsoleFormatterOptionsSchema = JsonSchema.FromType<JsonConsoleFormatterOptions>();
            schema.Definitions.Add(nameof(JsonConsoleFormatterOptions), jsonConsoleFormatterOptionsSchema);

            JsonSchema simpleConsoleFormatterOptionsSchema = JsonSchema.FromType<SimpleConsoleFormatterOptions>();
            schema.Definitions.Add(nameof(SimpleConsoleFormatterOptions), simpleConsoleFormatterOptionsSchema);

            JsonSchema systemdConsoleFormatterOptionsSchema = JsonSchema.FromType<ConsoleFormatterOptions>();
            schema.Definitions.Add(nameof(ConsoleFormatterOptions), systemdConsoleFormatterOptionsSchema);

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

            JsonSchema tempSchemaJson = new JsonSchema();
            JsonSchema tempSchemaSimple = new JsonSchema();
            JsonSchema tempSchemaSystemd = new JsonSchema();

            JsonSchemaProperty formatterNameConstraintPropertyJson = new JsonSchemaProperty();
            JsonSchemaProperty formatterOptionsConstraintPropertyJson = new JsonSchemaProperty();

            JsonSchema tempSchemaJson2 = new JsonSchema();
            tempSchemaJson2.Reference = jsonConsoleFormatterOptionsSchema;

            formatterOptionsConstraintPropertyJson.OneOf.Add(tempSchemaJson2);

            formatterNameConstraintPropertyJson.ExtensionData = new Dictionary<string, object>();
            formatterNameConstraintPropertyJson.ExtensionData.Add("const", "Json");

            tempSchemaJson.Properties.Add(nameof(ConsoleLoggerOptions.FormatterName), formatterNameConstraintPropertyJson);
            tempSchemaJson.Properties.Add(nameof(ConsoleLoggerOptions.FormatterOptions), formatterOptionsConstraintPropertyJson);
            tempSchemaJson.RequiredProperties.Add(nameof(ConsoleLoggerOptions.FormatterName));

            /////////////////////////

            JsonSchemaProperty formatterNameConstraintPropertySimple = new JsonSchemaProperty();
            JsonSchemaProperty formatterOptionsConstraintPropertySimple = new JsonSchemaProperty();

            JsonSchema tempSchemaSimple2 = new JsonSchema();
            tempSchemaSimple2.Reference = simpleConsoleFormatterOptionsSchema;

            formatterOptionsConstraintPropertySimple.OneOf.Add(tempSchemaSimple2);

            formatterNameConstraintPropertySimple.ExtensionData = new Dictionary<string, object>();
            formatterNameConstraintPropertySimple.ExtensionData.Add("const", "Simple");

            tempSchemaSimple.Properties.Add(nameof(ConsoleLoggerOptions.FormatterName), formatterNameConstraintPropertySimple);
            tempSchemaSimple.Properties.Add(nameof(ConsoleLoggerOptions.FormatterOptions), formatterOptionsConstraintPropertySimple);
            tempSchemaSimple.RequiredProperties.Add(nameof(ConsoleLoggerOptions.FormatterName));

            /////////////////////////

            JsonSchemaProperty formatterNameConstraintPropertySystemd = new JsonSchemaProperty();
            JsonSchemaProperty formatterOptionsConstraintPropertySystemd = new JsonSchemaProperty();

            JsonSchema tempSchemaSystemd2 = new JsonSchema();
            tempSchemaSystemd2.Reference = systemdConsoleFormatterOptionsSchema;

            formatterOptionsConstraintPropertySystemd.OneOf.Add(tempSchemaSystemd2);

            formatterNameConstraintPropertySystemd.ExtensionData = new Dictionary<string, object>();
            formatterNameConstraintPropertySystemd.ExtensionData.Add("const", "Systemd");

            tempSchemaSystemd.Properties.Add(nameof(ConsoleLoggerOptions.FormatterName), formatterNameConstraintPropertySystemd);
            tempSchemaSystemd.Properties.Add(nameof(ConsoleLoggerOptions.FormatterOptions), formatterOptionsConstraintPropertySystemd);
            tempSchemaSystemd.RequiredProperties.Add(nameof(ConsoleLoggerOptions.FormatterName));

            /////////////////////////

            schema.Definitions[nameof(ConsoleLoggerOptions)].AnyOf.Add(tempSchemaJson);
            schema.Definitions[nameof(ConsoleLoggerOptions)].AnyOf.Add(tempSchemaSimple);
            schema.Definitions[nameof(ConsoleLoggerOptions)].AnyOf.Add(tempSchemaSystemd);

            string schemaPayload = schema.ToJson();

            //Normalize newlines embedded into json
            schemaPayload = schemaPayload.Replace(@"\r\n", @"\n", StringComparison.Ordinal);
            return schemaPayload;
        }
    }
}
