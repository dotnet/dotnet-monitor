// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Tools.Monitor;
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
            var schema = new JsonSchema();
            var context = new GenerationContext(schema);
            context.SetRoot<RootOptions>();

            schema.Id = @"https://www.github.com/dotnet/dotnet-monitor";
            schema.Title = "DotnetMonitorConfiguration";

            //Allow other properties in the schema.
            schema.AdditionalPropertiesSchema = JsonSchema.CreateAnySchema();

            AddConsoleLoggerFormatterSubSchemas(context);

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

            string schemaPayload = schema.ToJson();

            //Normalize newlines embedded into json
            schemaPayload = schemaPayload.Replace(@"\r\n", @"\n", StringComparison.Ordinal);
            return schemaPayload;
        }

        private static void AddConsoleLoggerFormatterSubSchemas(GenerationContext context)
        {
            AddConsoleLoggerOptionsSubSchema<JsonConsoleFormatterOptions>(context, ConsoleLoggerFormat.Json);
            AddConsoleLoggerOptionsSubSchema<SimpleConsoleFormatterOptions>(context, ConsoleLoggerFormat.Simple);
            AddConsoleLoggerOptionsSubSchema<ConsoleFormatterOptions>(context, ConsoleLoggerFormat.Systemd);
            AddDefaultConsoleLoggerOptionsSubSchema(context);
        }

        private static void AddConsoleLoggerOptionsSubSchema<TOptions>(GenerationContext context, ConsoleLoggerFormat consoleLoggerFormat)
        {
            JsonSchema consoleLoggerOptionsSchema = new JsonSchema();
            consoleLoggerOptionsSchema.RequiredProperties.Add(nameof(ConsoleLoggerOptions.FormatterName));

            JsonSchemaProperty formatterOptionsProperty = AddDiscriminatedSubSchema(
                context.Schema.Definitions[nameof(ConsoleLoggerOptions)],
                nameof(ConsoleLoggerOptions.FormatterName),
                consoleLoggerFormat.ToString(),
                nameof(ConsoleLoggerOptions.FormatterOptions),
                consoleLoggerOptionsSchema);

            formatterOptionsProperty.Reference = context.AddTypeIfNotExist<TOptions>();
        }

        private static void AddDefaultConsoleLoggerOptionsSubSchema(GenerationContext context)
        {
            JsonSchema consoleLoggerOptionsSchema = new JsonSchema();

            JsonSchemaProperty formatterNameProperty = new JsonSchemaProperty();
            JsonSchemaProperty formatterOptionsProperty = new JsonSchemaProperty();

            formatterOptionsProperty.Reference = context.AddTypeIfNotExist<SimpleConsoleFormatterOptions>();

            formatterNameProperty.Type = JsonObjectType.Null;
            formatterNameProperty.Default = "Simple";

            consoleLoggerOptionsSchema.Properties.Add(nameof(ConsoleLoggerOptions.FormatterName), formatterNameProperty);
            consoleLoggerOptionsSchema.Properties.Add(nameof(ConsoleLoggerOptions.FormatterOptions), formatterOptionsProperty);

            context.Schema.Definitions[nameof(ConsoleLoggerOptions)].OneOf.Add(consoleLoggerOptionsSchema);
        }

        private static JsonSchemaProperty AddDiscriminatedSubSchema(
            JsonSchema parentSchema,
            string discriminatingPropertyName,
            string discriminatingPropertyValue,
            string discriminatedPropertyName,
            JsonSchema subSchema = null)
        {
            if (null == subSchema)
            {
                subSchema = new JsonSchema();
            }

            JsonSchemaProperty descriminatingProperty = new JsonSchemaProperty();
            descriminatingProperty.ExtensionData = new Dictionary<string, object>();
            descriminatingProperty.ExtensionData.Add("const", discriminatingPropertyValue);

            subSchema.Properties.Add(discriminatingPropertyName, descriminatingProperty);

            JsonSchemaProperty descriminatedProperty = new JsonSchemaProperty();

            subSchema.Properties.Add(discriminatedPropertyName, descriminatedProperty);

            parentSchema.OneOf.Add(subSchema);

            return descriminatedProperty;
        }

        private class GenerationContext
        {
            private readonly JsonSchemaGenerator _generator;
            private readonly JsonSchemaResolver _resolver;
            private readonly JsonSchemaGeneratorSettings _settings;

            public GenerationContext(JsonSchema rootSchema)
            {
                Schema = rootSchema;

                _settings = new JsonSchemaGeneratorSettings();
                _settings.SerializerSettings = new JsonSerializerSettings();
                _settings.SerializerSettings.Converters.Add(new StringEnumConverter());

                _resolver = new JsonSchemaResolver(rootSchema, _settings);

                _generator = new JsonSchemaGenerator(_settings);
            }

            public JsonSchema AddTypeIfNotExist<T>()
            {
                return _generator.Generate(typeof(T), _resolver);
            }

            public void SetRoot<T>()
            {
                _generator.Generate(Schema, typeof(T), _resolver);
            }

            public JsonSchema Schema { get; }
        }
    }
}