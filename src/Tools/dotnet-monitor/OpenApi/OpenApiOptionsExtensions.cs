// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Controllers;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Diagnostics.Tools.Monitor.OpenApi.Transformers;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;

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
                    schema.Type = "string";
                    schema.Format = "time-span";
                    schema.Example = new OpenApiString("00:00:30");
                    schema.Pattern = null;
                }
                return Task.CompletedTask;
            });

            // Make sure FileResult is represented as a string with binary format
            options.AddSchemaTransformer((schema, context, cancellationToken) => {
                if (context.JsonTypeInfo.Type == typeof(FileResult))
                {
                    schema.Reference = null;
                    schema.Type = "string";
                    schema.Format = "binary";
                    schema.Properties = new Dictionary<string, OpenApiSchema>();
                }
                return Task.CompletedTask;
            });

            // Make sure FileResult schema is inlined
            options.CreateSchemaReferenceId = (type) => 
                type.Type == typeof(FileResult) ? null : OpenApiOptions.CreateDefaultSchemaReferenceId(type);

            // Fix up nullable and uniqueItems
            options.AddSchemaTransformer((schema, context, cancellationToken) => {
                var type = context.JsonTypeInfo.Type;
                if (type == typeof(DotnetMonitorInfo))
                {
                    schema.Properties["capabilities"].Nullable = true;
                }
                else if (type == typeof(ExceptionsConfiguration))
                {
                    schema.Properties["include"].Nullable = true;
                    schema.Properties["exclude"].Nullable = true;

                    // Work around an issue where the OpenApi generator outputs an incorrect ref for the
                    // "exclude" ExceptionFilter when running on .NET 9.0.
                    schema.Properties["exclude"].Items.Reference = new OpenApiReference
                    {
                        Type = ReferenceType.Schema,
                        Id = nameof(ExceptionFilter)
                    };
                }
                else if (type == typeof(ValidationProblemDetails))
                {
                    schema.Properties["errors"].Nullable = true;
                }
                else if (type == typeof(OperationStatus) ||
                         type == typeof(OperationSummary))
                {
                    schema.Properties["tags"].UniqueItems = true;
                }
                return Task.CompletedTask;
            });

            // The OpenApi generator doesn't make nullable properties nullable in the schema,
            // but it does set the default value to null when the parameter had a default value of null.
            // This produces an invalid schema. To fix this, avoid setting a default value of null for non-nullable schemas.
            options.AddSchemaTransformer((schema, context, cancellationToken) => {
                if (!schema.Nullable && schema.Default is OpenApiNull)
                {
                    schema.Default = null;
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
                else if (type == typeof(LogsConfiguration))
                {
                    schema.AdditionalPropertiesAllowed = false;
                    schema.Properties["filterSpecs"].AdditionalProperties = new OpenApiSchema
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.Schema,
                            Id = nameof(LogLevel)
                        }
                    };
                }
                return Task.CompletedTask;
            });

            // Fix up "additionalProperties" for the response type of GetCollectionRulesDescription
            options.AddOperationTransformer((operation, context, cancellationToken) => {
                if (operation.OperationId == nameof(DiagController.GetCollectionRulesDescription))
                {
                    foreach (var response in operation.Responses)
                    {
                        if (response.Key == StatusCodeStrings.Status200Ok)
                        {
                            var schema = response.Value.Content[ContentTypes.ApplicationJson].Schema;
                            schema.AdditionalProperties.AdditionalPropertiesAllowed = false;
                        }
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
                    foreach (var parameter in operation.Parameters)
                    {
                        if (parameter.Name == "level")
                        {
                            parameter.Schema.Reference = new OpenApiReference
                            {
                                Type = ReferenceType.Schema,
                                Id = nameof(LogLevel)
                            };
                        }
                    }
                }
                return Task.CompletedTask;
            });
        }

        public static void AddBearerTokenAuthOption(this OpenApiOptions options, string securityDefinitionName)
        {
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                document.Components.SecuritySchemes.Add(securityDefinitionName, new OpenApiSecurityScheme
                {
                    Name = HeaderNames.Authorization,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = JwtBearerDefaults.AuthenticationScheme,
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = Strings.HelpDescription_SecurityDefinitionDescription_ApiKey
                });

                document.SecurityRequirements.Add(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = securityDefinitionName }
                        },
                        Array.Empty<string>()
                    }
                });

                return Task.CompletedTask;
            });
        }
    }
}
