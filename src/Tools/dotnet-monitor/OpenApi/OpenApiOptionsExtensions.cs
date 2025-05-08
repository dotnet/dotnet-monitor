// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.WebApi.Controllers;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Diagnostics.Tools.Monitor.OpenApi.Transformers;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Models.Interfaces;
using Microsoft.OpenApi.Models.References;
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
            options.AddSchemaTransformer((schema, context, cancellationToken) => {
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
            options.AddSchemaTransformer((schema, context, cancellationToken) => {
                if (context.JsonTypeInfo.Type == typeof(FileResult))
                {
                    schema.Type = JsonSchemaType.String;
                    schema.Format = "binary";
                    schema.Properties = new Dictionary<string, IOpenApiSchema>();
                }
                return Task.CompletedTask;
            });

            // Make sure int is represented as type: integer
            options.AddSchemaTransformer((schema, context, cancellationToken) => {
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
            options.AddDocumentTransformer((document, context, cancellationToken) => {
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
            options.AddSchemaTransformer((schema, context, cancellationToken) => {
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
            options.AddSchemaTransformer((schema, context, cancellationToken) => {
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
            options.AddSchemaTransformer((schema, context, cancellationToken) => {
                var descriptionAttribute = context.JsonTypeInfo.Type.GetCustomAttributes<DescriptionAttribute>().ToArray();
                if (descriptionAttribute.Length > 0)
                {
                    schema.Description = descriptionAttribute[0].Description;
                }
                return Task.CompletedTask;
            });

            options.AddDocumentTransformer((document, context, cancellationToken) => {
                document.Info.Title = "dotnet-monitor";
                document.Info.Version = "1.0";
                return Task.CompletedTask;
            });

            // Ensure LogLevel parameter is represented as a schema reference
            options.AddOperationTransformer((operation, context, cancellationToken) => {
                if (operation.OperationId == nameof(DiagController.CaptureLogs))
                {
                    if (operation.Parameters is IList<IOpenApiParameter> parameters)
                    {
                        foreach (var parameter in operation.Parameters)
                        {
                            if (parameter.Name == "level")
                            {
                                if (parameter is OpenApiParameter openApiParameter)
                                {
                                    openApiParameter.Schema = new OpenApiSchemaReference(nameof(LogLevel));
                                }
                            }
                        }
                    }
                }
                return Task.CompletedTask;
            });

            // Ensure enums have type: string
            options.AddSchemaTransformer((schema, context, cancellationToken) => {
                var type = context.JsonTypeInfo.Type;
                if (type.IsEnum)
                {
                    schema.Type = JsonSchemaType.String;

                    // Also treat enums with [Flags] as enums in the schema
                    if (type.GetCustomAttribute<FlagsAttribute>() != null)
                    {
                        schema.Enum = Enum.GetNames(type)
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
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    Type typeArg = type.GetGenericArguments().First();
                    if (typeArg.IsEnum)
                    {
                        schema.Type = JsonSchemaType.String | JsonSchemaType.Null;

                        schema.Enum = Enum.GetNames(typeArg)
                            .Select(name => JsonValue.Create(name))
                            .ToList<JsonNode>();
                    }
                }
                return Task.CompletedTask;
            });
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
