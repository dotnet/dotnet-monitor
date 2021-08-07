using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using NJsonSchema;
using NJsonSchema.Generation;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.Diagnostics.Monitoring.ConfigurationSchema
{
    internal sealed class SchemaGenerator
    {
        public string GenerateSchema()
        {
            var settings = new JsonSchemaGeneratorSettings();

            settings.SerializerSettings = new Newtonsoft.Json.JsonSerializerSettings();
            settings.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());

            JsonSchema schema = JsonSchema.FromType<RootOptions>(settings);
            schema.Id = @"https://www.github.com/dotnet/dotnet-monitor";
            schema.Title = "DotnetMonitorConfiguration";

            JsonSchema jsonConsoleFormatterOptions = JsonSchema.FromType<JsonConsoleFormatterOptions>();
            schema.Definitions.Add(nameof(JsonConsoleFormatterOptions), jsonConsoleFormatterOptions);

            JsonSchema simpleConsoleFormatterOptions = JsonSchema.FromType<SimpleConsoleFormatterOptions>();
            schema.Definitions.Add(nameof(SimpleConsoleFormatterOptions), simpleConsoleFormatterOptions);

            JsonSchema systemdConsoleFormatterOptions = JsonSchema.FromType<ConsoleFormatterOptions>();
            schema.Definitions.Add(nameof(ConsoleFormatterOptions), systemdConsoleFormatterOptions);

            //Allow other properties in the schema.
            schema.AdditionalPropertiesSchema = JsonSchema.CreateAnySchema();

            //TODO Figure out a better way to add object defaults
            schema.Definitions[nameof(EgressOptions)].Properties[nameof(EgressOptions.AzureBlobStorage)].Default = JsonSchema.CreateAnySchema();
            schema.Definitions[nameof(EgressOptions)].Properties[nameof(EgressOptions.FileSystem)].Default = JsonSchema.CreateAnySchema();
            schema.Definitions[nameof(EgressOptions)].Properties[nameof(EgressOptions.Properties)].Default = JsonSchema.CreateAnySchema();
            schema.Definitions[nameof(LoggingOptions)].Properties[nameof(LoggingOptions.LogLevel)].Default = JsonSchema.CreateAnySchema();
            schema.Definitions[nameof(LoggingOptions)].Properties[nameof(LoggingOptions.Console)].Default = JsonSchema.CreateAnySchema();
            schema.Definitions[nameof(LoggingOptions)].Properties[nameof(LoggingOptions.EventLog)].Default = JsonSchema.CreateAnySchema();
            schema.Definitions[nameof(LoggingOptions)].Properties[nameof(LoggingOptions.Debug)].Default = JsonSchema.CreateAnySchema();
            schema.Definitions[nameof(LoggingOptions)].Properties[nameof(LoggingOptions.EventSource)].Default = JsonSchema.CreateAnySchema();
            schema.Definitions[nameof(LogLevelOptions)].Properties[nameof(LogLevelOptions.LogLevel)].Default = JsonSchema.CreateAnySchema();
            schema.Definitions[nameof(ConsoleLoggerOptions)].Properties[nameof(ConsoleLoggerOptions.FormatterOptions)].Default = JsonSchema.CreateAnySchema();
            schema.Definitions[nameof(ConsoleLoggerOptions)].Properties[nameof(ConsoleLoggerOptions.LogLevel)].Default = JsonSchema.CreateAnySchema();

            //Make the default for each property an empty object.
            foreach (KeyValuePair<string, JsonSchemaProperty> kvp in schema.Properties)
            {
                kvp.Value.Default = JsonSchema.CreateAnySchema();
            }

            JsonSchema jsonConsoleLoggerOptionsSchema = GenerateConsoleLoggerOptionsSchema(jsonConsoleFormatterOptions, ConsoleLoggerFormat.Json);
            JsonSchema simpleConsoleLoggerOptionsSchema = GenerateConsoleLoggerOptionsSchema(simpleConsoleFormatterOptions, ConsoleLoggerFormat.Simple);
            JsonSchema systemdConsoleLoggerOptionsSchema = GenerateConsoleLoggerOptionsSchema(systemdConsoleFormatterOptions, ConsoleLoggerFormat.Systemd);
            JsonSchema defaultConsoleLoggerOptionsSchema = GenerateDefaultConsoleLoggerOptionsSchema(simpleConsoleFormatterOptions);

            schema.Definitions[nameof(ConsoleLoggerOptions)].AnyOf.Add(jsonConsoleLoggerOptionsSchema);
            schema.Definitions[nameof(ConsoleLoggerOptions)].AnyOf.Add(simpleConsoleLoggerOptionsSchema);
            schema.Definitions[nameof(ConsoleLoggerOptions)].AnyOf.Add(systemdConsoleLoggerOptionsSchema);
            schema.Definitions[nameof(ConsoleLoggerOptions)].AnyOf.Add(defaultConsoleLoggerOptionsSchema);

            string schemaPayload = schema.ToJson();

            //Normalize newlines embedded into json
            schemaPayload = schemaPayload.Replace(@"\r\n", @"\n", StringComparison.Ordinal);
            return schemaPayload;
        }

        public static JsonSchema GenerateConsoleLoggerOptionsSchema(JsonSchema consoleFormatterOptions, ConsoleLoggerFormat consoleLoggerFormat)
        {
            JsonSchema consoleLoggerOptionsSchema = new JsonSchema();

            JsonSchemaProperty formatterNameProperty = new JsonSchemaProperty();
            JsonSchemaProperty formatterOptionsProperty = new JsonSchemaProperty();

            JsonSchema formatterOptionsSchema = new JsonSchema();
            formatterOptionsSchema.Reference = consoleFormatterOptions;

            formatterOptionsProperty.Reference = formatterOptionsSchema;

            formatterNameProperty.ExtensionData = new Dictionary<string, object>();
            formatterNameProperty.ExtensionData.Add("const", consoleLoggerFormat.ToString());

            consoleLoggerOptionsSchema.Properties.Add(nameof(ConsoleLoggerOptions.FormatterName), formatterNameProperty);
            consoleLoggerOptionsSchema.Properties.Add(nameof(ConsoleLoggerOptions.FormatterOptions), formatterOptionsProperty);
            consoleLoggerOptionsSchema.RequiredProperties.Add(nameof(ConsoleLoggerOptions.FormatterName));

            return consoleLoggerOptionsSchema;
        }

        public static JsonSchema GenerateDefaultConsoleLoggerOptionsSchema(JsonSchema consoleFormatterOptions)
        {
            JsonSchema consoleLoggerOptionsSchema = new JsonSchema();

            JsonSchemaProperty formatterNameProperty = new JsonSchemaProperty();
            JsonSchemaProperty formatterOptionsProperty = new JsonSchemaProperty();
            
            JsonSchema formatterOptionsSchema = new JsonSchema();
            formatterOptionsSchema.Reference = consoleFormatterOptions;

            formatterOptionsProperty.Reference = formatterOptionsSchema;

            formatterNameProperty.ExtensionData = new Dictionary<string, object>();
            formatterNameProperty.ExtensionData.Add("const", JsonObjectType.Null);

            consoleLoggerOptionsSchema.Properties.Add(nameof(ConsoleLoggerOptions.FormatterName), formatterNameProperty);
            consoleLoggerOptionsSchema.Properties.Add(nameof(ConsoleLoggerOptions.FormatterOptions), formatterOptionsProperty);
            consoleLoggerOptionsSchema.Properties[nameof(ConsoleLoggerOptions.FormatterName)].Default = "Simple";

            return consoleLoggerOptionsSchema;
        }
    }
}
