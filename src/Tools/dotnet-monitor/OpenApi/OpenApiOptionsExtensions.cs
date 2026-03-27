// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.WebApi;

using Microsoft.Diagnostics.Monitoring.WebApi.Controllers;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Diagnostics.Tools.Monitor.OpenApi.Transformers;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.OpenApi
{
    internal static class OpenApiOptionsExtensions
    {
        public static void ConfigureMonitorOpenApiGen(this OpenApiOptions options)
        {
            options.AddDocumentTransformer<BadRequestResponseDocumentTransformer>();
            options.AddDocumentTransformer<UnauthorizedResponseDocumentTransformer>();
            options.AddDocumentTransformer<TooManyRequestsResponseDocumentTransformer>();

            options.AddOperationTransformer<BadRequestResponseOperationTransformer>();
            options.AddOperationTransformer<RemoveFailureContentTypesOperationTransformer>();
            options.AddOperationTransformer<TooManyRequestsResponseOperationTransformer>();
            options.AddOperationTransformer<UnauthorizedResponseOperationTransformer>();

            // Make sure TimeSpan is represented as a string instead of a full object type
            options.AddSchemaTransformer((schema, context, cancellationToken) =>
            {
                if (context.JsonTypeInfo.Type == typeof(TimeSpan?))
                {
                    schema.Type = JsonSchemaType.String | JsonSchemaType.Null;
                    schema.Format = "time-span";
                    schema.Example = JsonValue.Create("00:00:30");
                    schema.Pattern = null;
                }
                return Task.CompletedTask;
            });

            // Make sure FileResult is represented as a string with binary format
            options.AddSchemaTransformer((schema, context, cancellationToken) =>
            {
                if (context.JsonTypeInfo.Type == typeof(FileResult))
                {
                    schema.Type = JsonSchemaType.String;
                    schema.Format = "binary";
                    schema.Properties = new Dictionary<string, IOpenApiSchema>();
                }
                return Task.CompletedTask;
            });

            // Make sure int is represented as type: integer
            options.AddSchemaTransformer((schema, context, cancellationToken) =>
            {
                var type = context.JsonTypeInfo.Type;
                if (type == typeof(int) || type == typeof(int?))
                {
                    if (!schema.Type?.HasFlag(JsonSchemaType.Integer) != true)
                    {
                        schema.Type = JsonSchemaType.Integer;
                        schema.Pattern = null;
                    }
                }
                return Task.CompletedTask;
            });

            // Make sure FileResult schema is inlined
            options.CreateSchemaReferenceId = (type) =>
                type.Type == typeof(FileResult) ? null : OpenApiOptions.CreateDefaultSchemaReferenceId(type);

            // Fix ExceptionFilter schema to work around https://github.com/dotnet/aspnetcore/issues/61194
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                OpenApiComponents components = document.Components ??= new OpenApiComponents();
                IDictionary<string, IOpenApiSchema> schemas = components.Schemas ??= new Dictionary<string, IOpenApiSchema>();
                schemas[nameof(ExceptionFilter)] = new OpenApiSchema
                {
                    Type = JsonSchemaType.Object,
                    Properties = new Dictionary<string, IOpenApiSchema>
                    {
                        { "exceptionType", new OpenApiSchema { Type = JsonSchemaType.String | JsonSchemaType.Null } },
                        { "moduleName", new OpenApiSchema { Type = JsonSchemaType.String | JsonSchemaType.Null } },
                        { "typeName", new OpenApiSchema { Type = JsonSchemaType.String | JsonSchemaType.Null } },
                        { "methodName", new OpenApiSchema { Type = JsonSchemaType.String | JsonSchemaType.Null } }
                    },
                    AdditionalPropertiesAllowed = false
                };
                return Task.CompletedTask;
            });

            // Fix up nullable and uniqueItems
            options.AddSchemaTransformer((schema, context, cancellationToken) =>
            {
                var type = context.JsonTypeInfo.Type;
                if (type == typeof(CaptureParametersConfiguration))
                {
                    if (schema.Properties?["captureLimit"] is OpenApiSchema captureLimitSchema)
                    {
                        captureLimitSchema.Type |= JsonSchemaType.Null;
                    }
                }
                else if (type == typeof(DotnetMonitorInfo))
                {
                    if (schema.Properties?["capabilities"] is OpenApiSchema capabilitiesSchema)
                    {
                        capabilitiesSchema.Type |= JsonSchemaType.Null;
                    }
                }
                else if (type == typeof(ExceptionsConfiguration))
                {
                    if (schema.Properties?["include"] is OpenApiSchema includeSchema)
                    {
                        includeSchema.Type |= JsonSchemaType.Null;
                    }

                    if (schema.Properties?["exclude"] is OpenApiSchema excludeSchema)
                    {
                        excludeSchema.Type |= JsonSchemaType.Null;
                    }
                }
                else if (type == typeof(OperationStatus) ||
                         type == typeof(OperationSummary))
                {
                    if (schema.Properties?["tags"] is OpenApiSchema tagsSchema)
                    {
                        tagsSchema.UniqueItems = true;
                    }
                }
                else if (type == typeof(ProblemDetails))
                {
                    if (schema.Properties?["status"] is OpenApiSchema statusSchema)
                    {
                        statusSchema.Type |= JsonSchemaType.Null;
                    }
                }
                else if (type == typeof(ValidationProblemDetails))
                {
                    if (schema.Properties?["errors"] is OpenApiSchema errorsSchema)
                    {
                        errorsSchema.Type |= JsonSchemaType.Null;
                    }

                    if (schema.Properties?["status"] is OpenApiSchema statusSchema)
                    {
                        statusSchema.Type |= JsonSchemaType.Null;
                    }
                }
                return Task.CompletedTask;
            });

            // Fix up "additionalProperties"
            options.AddSchemaTransformer((schema, context, cancellationToken) =>
            {
                var type = context.JsonTypeInfo.Type;
                if (type == typeof(CaptureParametersConfiguration) ||
                    type == typeof(CollectionRuleDetailedDescription) ||
                    type == typeof(DotnetMonitorInfo) ||
                    type == typeof(EventMetricsConfiguration) ||
                    type == typeof(EventMetricsMeter) ||
                    type == typeof(EventMetricsProvider) ||
                    type == typeof(EventPipeConfiguration) ||
                    type == typeof(EventPipeProvider) ||
                    type == typeof(ExceptionFilter) ||
                    type == typeof(ExceptionsConfiguration) ||
                    type == typeof(LogsConfiguration) ||
                    type == typeof(MethodDescription) ||
                    type == typeof(MonitorCapability) ||
                    type == typeof(OperationError) ||
                    type == typeof(OperationProcessInfo) ||
                    type == typeof(OperationStatus) ||
                    type == typeof(OperationSummary) ||
                    type == typeof(ProcessIdentifier) ||
                    type == typeof(ProcessInfo))
                {
                    schema.AdditionalPropertiesAllowed = false;
                }
                else if (type == typeof(ProblemDetails) ||
                         type == typeof(ValidationProblemDetails))
                {
                    schema.AdditionalProperties = new OpenApiSchema();
                }
                else if (type == typeof(Dictionary<string, CollectionRuleDescription>))
                {
                    if (schema.AdditionalProperties is OpenApiSchema additionalProperties)
                    {
                        additionalProperties.AdditionalPropertiesAllowed = false;
                    }
                }
                return Task.CompletedTask;
            });

            // Add missing descriptions on types that have DescriptionAttribute
            options.AddSchemaTransformer((schema, context, cancellationToken) =>
            {
                var descriptionAttribute = context.JsonTypeInfo.Type.GetCustomAttributes<DescriptionAttribute>().ToArray();
                if (descriptionAttribute.Length > 0)
                {
                    schema.Description = descriptionAttribute[0].Description;
                }
                return Task.CompletedTask;
            });

            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                document.Info.Title = "dotnet-monitor";
                document.Info.Version = "1.0";
                return Task.CompletedTask;
            });

            // Ensure referenced enum component schemas exist for EnumBinding-backed query parameters.
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                OpenApiComponents components = document.Components ??= new OpenApiComponents();
                IDictionary<string, IOpenApiSchema> schemas = components.Schemas ??= new Dictionary<string, IOpenApiSchema>();

                EnsureEnumSchema<DumpType>(schemas, defaultValue: DumpType.WithHeap);
                EnsureEnumSchema<TraceProfile>(schemas, defaultValue: TraceProfile.Cpu | TraceProfile.Http | TraceProfile.Metrics | TraceProfile.GcCollect);

                //TODO Figure out why this is the only nullable enum
                EnsureEnumSchema<LogLevel>(schemas, nullable: true);

                return Task.CompletedTask;
            });

            // Ensure enum query parameters that use EnumBinding<T> are represented as enums.
            options.AddOperationTransformer((operation, context, cancellationToken) =>
            {
                if (operation.Parameters is IList<IOpenApiParameter> parameters)
                {
                    foreach (var parameter in parameters)
                    {
                        if (parameter is not OpenApiParameter openApiParameter)
                        {
                            continue;
                        }

                        if (operation.OperationId == nameof(DiagController.CaptureLogs) && parameter.Name == "level")
                        {
                            openApiParameter.Schema = new OpenApiSchemaReference(nameof(LogLevel));
                        }
                        else if (operation.OperationId == nameof(DiagController.CaptureDump) && parameter.Name == "type")
                        {
                            openApiParameter.Schema = new OpenApiSchemaReference(nameof(DumpType));
                        }
                        else if (operation.OperationId == nameof(DiagController.CaptureTrace) && parameter.Name == "profile")
                        {
                            openApiParameter.Schema = new OpenApiSchemaReference(nameof(TraceProfile));
                        }
                    }
                }
                return Task.CompletedTask;
            });

            //Ensure enums have type: string
            options.AddSchemaTransformer((schema, context, cancellationToken) =>
            {
                var type = context.JsonTypeInfo.Type;
                if (TryGetEnumType(type, out Type? enumType, out bool isNullableEnum))
                {
                    schema.Type = isNullableEnum ? JsonSchemaType.String | JsonSchemaType.Null : JsonSchemaType.String;
                    if (enumType == null)
                    {
                        throw new InvalidOperationException("Unexpected null Type");
                    }
                    // EnumBinding<TEnum> values are represented as strings in query parameters.
                    // Populate enum values explicitly so OpenAPI documents them as enums.
                    if (IsEnumBindingType(type) || enumType.GetCustomAttribute<FlagsAttribute>() != null)
                    {
                        schema.Enum = Enum.GetNames(enumType)
                            .Select(name => JsonValue.Create(name))
                            .ToList<JsonNode>();
                    }
                }
                return Task.CompletedTask;
            });

            // Ensure nullable enums have correct schema
            options.AddSchemaTransformer((schema, context, cancellationToken) =>
            {
                var type = context.JsonTypeInfo.Type;
                if (TryGetNullableEnumType(type, out Type? enumType))
                {
                    if (enumType == null)
                    {
                        throw new InvalidOperationException("Unexpected null type");
                    }
                    schema.Type = JsonSchemaType.String | JsonSchemaType.Null;

                    if (!IsEnumBindingType(type))
                    {
                        schema.Enum = Enum.GetNames(enumType)
                            .Select(name => JsonValue.Create(name))
                            .ToList<JsonNode>();
                    }
                }
                return Task.CompletedTask;
            });
        }

        private static bool TryGetEnumType(Type type, out Type? enumType, out bool isNullableEnum)
        {
            if (type.IsEnum)
            {
                enumType = type;
                isNullableEnum = false;
                return true;
            }

            if (TryGetNullableEnumType(type, out enumType))
            {
                isNullableEnum = true;
                return true;
            }

            if (TryGetEnumBindingEnumType(type, out enumType, out isNullableEnum))
            {
                return true;
            }

            enumType = null;
            isNullableEnum = false;
            return false;
        }

        private static bool TryGetNullableEnumType(Type type, out Type? enumType)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                Type typeArg = type.GetGenericArguments().First();
                if (typeArg.IsEnum)
                {
                    enumType = typeArg;
                    return true;
                }
            }

            enumType = null;
            return false;
        }

        private static bool TryGetEnumBindingEnumType(Type type, out Type? enumType, out bool isNullableEnum)
        {
            isNullableEnum = false;
            Type checkType = type;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                checkType = type.GetGenericArguments().First();
                isNullableEnum = true;
            }

            if (IsEnumBindingType(checkType))
            {
                Type typeArg = checkType.GetGenericArguments().First();
                if (typeArg.IsEnum)
                {
                    enumType = typeArg;
                    return true;
                }
            }

            enumType = null;
            isNullableEnum = false;
            return false;
        }

        private static bool IsEnumBindingType(Type type)
        {
            return type.IsGenericType
                && type.GetGenericTypeDefinition() == typeof(EnumBinding<>);
        }

        private static void EnsureEnumSchema<TEnum>(IDictionary<string, IOpenApiSchema> schemas, bool nullable = false,
            TEnum? defaultValue = null) where TEnum : struct, Enum
        {
            string schemaName = typeof(TEnum).Name;
            if (!schemas.TryGetValue(schemaName, out IOpenApiSchema? lookupSchema))
            {
                lookupSchema = new OpenApiSchema();
                schemas.Add(schemaName, lookupSchema);
            }

            if (lookupSchema is OpenApiSchema schema)
            {
                schema.Type = JsonSchemaType.String | (nullable ? JsonSchemaType.Null : 0);
                schema.Enum = Enum.GetNames(typeof(TEnum))
                    .Select(name => JsonValue.Create(name))
                    .ToList<JsonNode>();

                if (defaultValue != null)
                {
                    schema.Default = JsonValue.Create(defaultValue.Value.ToString());
                }
            }
        }

        public static void AddBearerTokenAuthOption(this OpenApiOptions options, string securityDefinitionName)
        {
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                OpenApiComponents components = document.Components ??= new OpenApiComponents();

                IDictionary<string, IOpenApiSecurityScheme> securitySchemes = components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                securitySchemes.Add(securityDefinitionName, new OpenApiSecurityScheme
                {
                    Name = HeaderNames.Authorization,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = JwtBearerDefaults.AuthenticationScheme,
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = Strings.HelpDescription_SecurityDefinitionDescription_ApiKey
                });

                IList<OpenApiSecurityRequirement> securityRequirements = document.Security ??= new List<OpenApiSecurityRequirement>();
                securityRequirements.Add(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecuritySchemeReference(securityDefinitionName),
                        []
                    }
                });

                return Task.CompletedTask;
            });
        }
    }
}
