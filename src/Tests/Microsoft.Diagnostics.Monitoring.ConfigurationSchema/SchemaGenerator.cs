// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.AzureBlobStorage;
using Microsoft.Diagnostics.Monitoring.Extension.S3Storage;
using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers.EventCounterShortcuts;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NJsonSchema;
using NJsonSchema.Generation;
using NJsonSchema.NewtonsoftJson.Generation;
using System;
using System.Collections.Generic;
using System.Linq;

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

            AddCollectionRuleSchemas(context);
            AddConsoleLoggerFormatterSubSchemas(context);
            AddDiagnosticPortSchema(context, schema);
            AddEgressExtensionSchemas(context);

            //TODO Figure out a better way to add object defaults
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
            schema.Definitions[nameof(CollectionRuleOptions)].Properties[nameof(CollectionRuleOptions.Limits)].Default = JsonSchema.CreateAnySchema();

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

        private static void AddDiagnosticPortSchema(GenerationContext context, JsonSchema schema)
        {
            JsonSchema stringSchema = new JsonSchema() { Type = JsonObjectType.String };
            schema.Properties[nameof(RootOptions.DiagnosticPort)].OneOf.Add(stringSchema);
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

        private static void AddCollectionRuleSchemas(GenerationContext context)
        {
            JsonSchema actionTypeSchema = new JsonSchema();
            actionTypeSchema.Type = JsonObjectType.String;
            context.Schema.Definitions.Add("CollectionRuleActionType", actionTypeSchema);

            JsonSchema collectionRuleActionOptionsTypeSubSchema = new JsonSchema();
            collectionRuleActionOptionsTypeSubSchema.Reference = actionTypeSchema;

            JsonSchema collectionRuleActionOptionsSchema = context.Schema.Definitions[nameof(CollectionRuleActionOptions)];
            collectionRuleActionOptionsSchema.Properties[nameof(CollectionRuleActionOptions.Type)].OneOf.Add(collectionRuleActionOptionsTypeSubSchema);

            AddCollectionRuleActionSchema<CollectDumpOptions>(context, actionTypeSchema, KnownCollectionRuleActions.CollectDump);
            AddCollectionRuleActionSchema<CollectExceptionsOptions>(context, actionTypeSchema, KnownCollectionRuleActions.CollectExceptions);
            AddCollectionRuleActionSchema<CollectGCDumpOptions>(context, actionTypeSchema, KnownCollectionRuleActions.CollectGCDump);
            AddCollectionRuleActionSchema<CollectLiveMetricsOptions>(context, actionTypeSchema, KnownCollectionRuleActions.CollectLiveMetrics);
            AddCollectionRuleActionSchema<CollectLogsOptions>(context, actionTypeSchema, KnownCollectionRuleActions.CollectLogs);
            AddCollectionRuleActionSchema<CollectStacksOptions>(context, actionTypeSchema, KnownCollectionRuleActions.CollectStacks);
            AddCollectionRuleActionSchema<CollectTraceOptions>(context, actionTypeSchema, KnownCollectionRuleActions.CollectTrace);
            AddCollectionRuleActionSchema<ExecuteOptions>(context, actionTypeSchema, KnownCollectionRuleActions.Execute);
            AddCollectionRuleActionSchema<LoadProfilerOptions>(context, actionTypeSchema, KnownCollectionRuleActions.LoadProfiler);
            AddCollectionRuleActionSchema<SetEnvironmentVariableOptions>(context, actionTypeSchema, KnownCollectionRuleActions.SetEnvironmentVariable);
            AddCollectionRuleActionSchema<GetEnvironmentVariableOptions>(context, actionTypeSchema, KnownCollectionRuleActions.GetEnvironmentVariable);

            JsonSchema triggerTypeSchema = new JsonSchema();
            triggerTypeSchema.Type = JsonObjectType.String;
            context.Schema.Definitions.Add("CollectionRuleTriggerType", triggerTypeSchema);

            JsonSchema collectionRuleTriggerOptionsTypeSubSchema = new JsonSchema();
            collectionRuleTriggerOptionsTypeSubSchema.Reference = triggerTypeSchema;

            JsonSchema collectionRuleTriggerOptionsSchema = context.Schema.Definitions[nameof(CollectionRuleTriggerOptions)];
            collectionRuleTriggerOptionsSchema.Properties[nameof(CollectionRuleTriggerOptions.Type)].OneOf.Add(collectionRuleTriggerOptionsTypeSubSchema);

            AddCollectionRuleTriggerSchema<AspNetRequestCountOptions>(context, triggerTypeSchema, KnownCollectionRuleTriggers.AspNetRequestCount);
            AddCollectionRuleTriggerSchema<AspNetRequestDurationOptions>(context, triggerTypeSchema, KnownCollectionRuleTriggers.AspNetRequestDuration);
            AddCollectionRuleTriggerSchema<AspNetResponseStatusOptions>(context, triggerTypeSchema, KnownCollectionRuleTriggers.AspNetResponseStatus);
            AddCollectionRuleTriggerSchema<EventCounterOptions>(context, triggerTypeSchema, KnownCollectionRuleTriggers.EventCounter);
            AddCollectionRuleTriggerSchema<CPUUsageOptions>(context, triggerTypeSchema, KnownCollectionRuleTriggers.CPUUsage);
            AddCollectionRuleTriggerSchema<GCHeapSizeOptions>(context, triggerTypeSchema, KnownCollectionRuleTriggers.GCHeapSize);
            AddCollectionRuleTriggerSchema<ThreadpoolQueueLengthOptions>(context, triggerTypeSchema, KnownCollectionRuleTriggers.ThreadpoolQueueLength);
            AddCollectionRuleTriggerSchema<EventMeterOptions>(context, triggerTypeSchema, KnownCollectionRuleTriggers.EventMeter);
            AddCollectionRuleTriggerSchema(context, triggerTypeSchema, KnownCollectionRuleTriggers.Startup);
        }

        private static void AddCollectionRuleActionSchema<TOptions>(GenerationContext context, JsonSchema actionTypeSchema, string actionType)
        {
            JsonSchema subSchema = new JsonSchema();

            JsonSchemaProperty settingsProperty = AddDiscriminatedSubSchema(
                context.Schema.Definitions[nameof(CollectionRuleActionOptions)],
                nameof(CollectionRuleActionOptions.Type),
                actionType,
                nameof(CollectionRuleActionOptions.Settings),
                subSchema);

            settingsProperty.Reference = context.AddTypeIfNotExist<TOptions>();

            var propertyNames = GetCollectionRuleDefaultsPropertyNames();

            // Don't require properties that have a corresponding collection rule default
            foreach (var propName in propertyNames)
            {
                settingsProperty.Reference.RequiredProperties.Remove(propName);
            }

            if (settingsProperty.Reference.RequiredProperties.Count > 0)
            {
                subSchema.RequiredProperties.Add(nameof(CollectionRuleActionOptions.Settings));
            }

            actionTypeSchema.Enumeration.Add(actionType);
        }

        private static void AddCollectionRuleTriggerSchema(GenerationContext context, JsonSchema triggerTypeSchema, string triggerType)
        {
            JsonSchemaProperty settingsProperty = AddDiscriminatedSubSchema(
                context.Schema.Definitions[nameof(CollectionRuleTriggerOptions)],
                nameof(CollectionRuleTriggerOptions.Type),
                triggerType,
                nameof(CollectionRuleTriggerOptions.Settings));

            settingsProperty.Type = JsonObjectType.Null;

            triggerTypeSchema.Enumeration.Add(triggerType);
        }

        private static void AddCollectionRuleTriggerSchema<TOptions>(GenerationContext context, JsonSchema triggerTypeSchema, string triggerType)
        {
            JsonSchema subSchema = new JsonSchema();

            JsonSchemaProperty settingsProperty = AddDiscriminatedSubSchema(
                context.Schema.Definitions[nameof(CollectionRuleTriggerOptions)],
                nameof(CollectionRuleTriggerOptions.Type),
                triggerType,
                nameof(CollectionRuleTriggerOptions.Settings),
                subSchema);

            settingsProperty.Reference = context.AddTypeIfNotExist<TOptions>();

            IEnumerable<string> propertyNames = GetCollectionRuleDefaultsPropertyNames();

            // Don't require properties that have a corresponding collection rule default
            foreach (var propName in propertyNames)
            {
                settingsProperty.Reference.RequiredProperties.Remove(propName);
            }

            if (settingsProperty.Reference.RequiredProperties.Count > 0)
            {
                subSchema.RequiredProperties.Add(nameof(CollectionRuleTriggerOptions.Settings));
            }

            triggerTypeSchema.Enumeration.Add(triggerType);
        }

        private static IEnumerable<string> GetCollectionRuleDefaultsPropertyNames()
        {
            List<string> propertyNames = new();

            foreach (var defaultsType in typeof(CollectionRuleDefaultsOptions).GetProperties())
            {
                propertyNames.AddRange(defaultsType.PropertyType.GetProperties().Select(x => x.Name));
            }

            return propertyNames;
        }

        private static void AddEgressExtensionSchemas(GenerationContext context)
        {
            AddEgressExtensionSchema<AzureBlobEgressProviderOptions>(
                context,
                AzureBlobStorage.Constants.AzureBlobStorageProviderName,
                "Mapping of Azure blob storage egress provider names to their options.");
            AddEgressExtensionSchema<S3StorageEgressProviderOptions>(
                context,
                Extension.S3Storage.Constants.S3StorageProviderName,
                "Mapping of S3 storage egress provider names to their options.");
        }

        private static void AddEgressExtensionSchema<TOptions>(GenerationContext context, string name, string description)
        {
            JsonSchema egressProviderOptionsSchema = context.AddTypeIfNotExist<TOptions>();

            JsonSchemaProperty egressProviderProperty = new JsonSchemaProperty();
            egressProviderProperty.Type = JsonObjectType.Null | JsonObjectType.Object;
            egressProviderProperty.Description = description;
            egressProviderProperty.AdditionalPropertiesSchema = new JsonSchema()
            {
                Reference = egressProviderOptionsSchema
            };
            egressProviderProperty.Default = JsonSchema.CreateAnySchema();

            JsonSchemaProperty egressProperty = context.Schema.Properties[nameof(RootOptions.Egress)];
            egressProperty.ActualTypeSchema.Properties.Add(name, egressProviderProperty);
        }

        private static JsonSchemaProperty AddDiscriminatedSubSchema(
            JsonSchema parentSchema,
            string discriminatingPropertyName,
            string discriminatingPropertyValue,
            string discriminatedPropertyName,
            JsonSchema? subSchema = null)
        {
            if (null == subSchema)
            {
                subSchema = new JsonSchema();
            }

            JsonSchemaProperty discriminatingProperty = new JsonSchemaProperty();
            discriminatingProperty.ExtensionData = new Dictionary<string, object?>();
            discriminatingProperty.ExtensionData.Add("const", discriminatingPropertyValue);

            subSchema.Properties.Add(discriminatingPropertyName, discriminatingProperty);

            JsonSchemaProperty discriminatedProperty = new JsonSchemaProperty();

            subSchema.Properties.Add(discriminatedPropertyName, discriminatedProperty);

            parentSchema.OneOf.Add(subSchema);

            return discriminatedProperty;
        }

        private sealed class GenerationContext
        {
            private readonly JsonSchemaGenerator _generator;
            private readonly JsonSchemaResolver _resolver;
            private readonly JsonSchemaGeneratorSettings _settings;

            public GenerationContext(JsonSchema rootSchema)
            {
                Schema = rootSchema;

                JsonSerializerSettings serializerSettings = new();
                serializerSettings.Converters.Add(new StringEnumConverter());

                _settings = new NewtonsoftJsonSchemaGeneratorSettings
                {
                    SerializerSettings = serializerSettings
                };

                _settings.SchemaProcessors.Add(new ExperimentalSchemaProcessor());

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
