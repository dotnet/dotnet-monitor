// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Controllers;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Diagnostics.Tools.Monitor.Swagger;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Swagger;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.IO.Compression;
using System.Text.Json.Serialization;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // AddControllers is sufficient because the tool does not use Razor nor Views.
            services.ConfigureHttpJsonOptions(options => {
                options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var details = new ValidationProblemDetails(context.ModelState);
                    var result = new BadRequestObjectResult(details);
                    result.ContentTypes.Add(ContentTypes.ApplicationProblemJson);
                    return result;
                };
            });

            services.Configure<BrotliCompressionProviderOptions>(options =>
            {
                options.Level = CompressionLevel.Optimal;
            });

            services.AddResponseCompression(configureOptions =>
            {
                configureOptions.Providers.Add<BrotliCompressionProvider>();
                configureOptions.MimeTypes = new List<string> { ContentTypes.ApplicationOctetStream };
            });

            services.ConfigureCors(Configuration);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public static void Configure(IApplicationBuilder app, IWebHostEnvironment env, IOptions<CorsConfigurationOptions> corsOptions)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseSwagger();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            if (!string.IsNullOrEmpty(corsOptions.Value.AllowedOrigins))
            {
                app.UseCors(builder => builder.WithOrigins(corsOptions.Value.GetOrigins()).AllowAnyHeader().AllowAnyMethod());
            }

            // Disable response compression due to ASP.NET 6.0 bug:
            // https://github.com/dotnet/aspnetcore/issues/36960
            //app.UseResponseCompression();

            app.UseEndpoints(builder =>
            {
                builder.MapControllers();

                var serviceProvider = builder.ServiceProvider;

                // OperationsController:
                {
                    var logger = serviceProvider.GetRequiredService<ILogger<OperationsController>>();
                    var operationsController = new OperationsController(logger, serviceProvider);

                    // GetOperations
                    builder.MapGet($"{OperationsController.ControllerName}/{nameof(OperationsController.GetOperations)}", (
                        [FromQuery]
                        int? pid,
                        [FromQuery]
                        Guid? uid,
                        [FromQuery]
                        string? name,
                        [FromQuery]
                        string? tags) =>
                            operationsController.GetOperations(pid, uid, name, tags))
                    .WithName(nameof(OperationsController.GetOperations))
                    .RequireHostRestriction()
                    .RequireAuthorization(AuthConstants.PolicyName)
                    .Produces(StatusCodes.Status401Unauthorized)
                    .Produces<IEnumerable<Monitoring.WebApi.Models.OperationSummary>>(StatusCodes.Status200OK);

                    // GetOperationStatus
                    builder.MapGet($"{OperationsController.ControllerName}/{nameof(OperationsController.GetOperationStatus)}/{{operationId}}", (
                        Guid operationId) =>
                        operationsController.GetOperationStatus(operationId))
                    .WithName(nameof(OperationsController.GetOperationStatus))
                    .RequireHostRestriction()
                    .RequireAuthorization(AuthConstants.PolicyName)
                    .Produces(StatusCodes.Status401Unauthorized)
                    .Produces<OperationStatus>(StatusCodes.Status200OK)
                    .Produces<OperationStatus>(StatusCodes.Status201Created);

                    // CancelOperation
                    builder.MapDelete($"{OperationsController.ControllerName}/{nameof(OperationsController.CancelOperation)}/{{operationId}}", (
                        Guid operationId) =>
                            operationsController.CancelOperation(operationId))
                    .WithName(nameof(OperationsController.CancelOperation))
                    .RequireHostRestriction()
                    .RequireAuthorization(AuthConstants.PolicyName)
                    .Produces(StatusCodes.Status401Unauthorized)
                    .Produces(StatusCodes.Status200OK)
                    .Produces(StatusCodes.Status202Accepted);
                }

                var diagLogger = serviceProvider.GetRequiredService<ILogger<DiagController>>();
                var diagController = new DiagController(serviceProvider, diagLogger);

                // DiagController
                {
                    // GetProcesses
                    builder.MapGet("processes", () => diagController.GetProcesses())
                        .WithName(nameof(DiagController.GetProcesses))
                        .RequireHostRestriction()
                        .RequireAuthorization(AuthConstants.PolicyName)
                        .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest, ContentTypes.ApplicationProblemJson)
                        .Produces(StatusCodes.Status401Unauthorized)
                        .Produces<IEnumerable<ProcessIdentifier>>(StatusCodes.Status200OK)
                        .ProducesProblem(StatusCodes.Status400BadRequest);

                    // GetProcessInfo
                    builder.MapGet("process", (
                        [FromQuery]
                        int? pid,
                        [FromQuery]
                        Guid? uid,
                        [FromQuery]
                        string? name) =>
                            diagController.GetProcessInfo(pid, uid, name))
                        .WithName(nameof(DiagController.GetProcessInfo))
                        .RequireHostRestriction()
                        .RequireAuthorization(AuthConstants.PolicyName)
                        .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest, ContentTypes.ApplicationProblemJson)
                        .Produces(StatusCodes.Status401Unauthorized)
                        .Produces<ProcessInfo>(StatusCodes.Status200OK)
                        .ProducesProblem(StatusCodes.Status400BadRequest);

                    // GetProcessEnvironment
                    builder.MapGet("env", (
                        [FromQuery]
                        int? pid,
                        [FromQuery]
                        Guid? uid,
                        [FromQuery]
                        string? name) =>
                            diagController.GetProcessEnvironment(pid, uid, name))
                        .WithName(nameof(DiagController.GetProcessEnvironment))
                        .RequireHostRestriction()
                        .RequireAuthorization(AuthConstants.PolicyName)
                        .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest, ContentTypes.ApplicationProblemJson)
                        .Produces(StatusCodes.Status401Unauthorized)
                        .Produces<Dictionary<string, string>>(StatusCodes.Status200OK)
                        .ProducesProblem(StatusCodes.Status400BadRequest);

                    // CaptureDump
                    builder.MapGet("dump", (
                        [FromQuery]
                        int? pid,
                        [FromQuery]
                        Guid? uid,
                        [FromQuery]
                        string? name,
                        [FromQuery]
                        DumpType type = DumpType.WithHeap,
                        [FromQuery]
                        string? egressProvider = null,
                        [FromQuery]
                        string? tags = null) =>
                            diagController.CaptureDump(pid, uid, name, type, egressProvider, tags))
                        .WithName(nameof(DiagController.CaptureDump))
                        .RequireHostRestriction()
                        .RequireAuthorization(AuthConstants.PolicyName)
                        .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest, ContentTypes.ApplicationProblemJson)
                        .Produces(StatusCodes.Status401Unauthorized)
                        .Produces<ProblemDetails>(StatusCodes.Status429TooManyRequests)
                        .Produces<FileResult>(StatusCodes.Status200OK, ContentTypes.ApplicationOctetStream)
                        .Produces(StatusCodes.Status202Accepted)
                        .ProducesProblem(StatusCodes.Status400BadRequest)
                        .RequireEgressValidation();

                    // CapturGcDump
                    builder.MapGet("gcdump", (
                        [FromQuery]
                        int? pid,
                        [FromQuery]
                        Guid? uid,
                        [FromQuery]
                        string? name,
                        [FromQuery]
                        string? egressProvider,
                        [FromQuery]
                        string? tags) =>
                            diagController.CaptureGcDump(pid, uid, name, egressProvider, tags))
                        .WithName(nameof(DiagController.CaptureGcDump))
                        .RequireHostRestriction()
                        .RequireAuthorization(AuthConstants.PolicyName)
                        .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest, ContentTypes.ApplicationProblemJson)
                        .Produces(StatusCodes.Status401Unauthorized)
                        .Produces<ProblemDetails>(StatusCodes.Status429TooManyRequests)
                        .Produces<FileResult>(StatusCodes.Status200OK, ContentTypes.ApplicationOctetStream)
                        .Produces(StatusCodes.Status202Accepted)
                        .ProducesProblem(StatusCodes.Status400BadRequest)
                        .RequireEgressValidation();

                    // CaptureTrace
                    builder.MapGet("trace", (
                        [FromQuery]
                        int? pid,
                        [FromQuery]
                        Guid? uid,
                        [FromQuery]
                        string? name,
                        [FromQuery]
                        TraceProfile profile = DiagController.DefaultTraceProfiles,
                        [FromQuery][Range(-1, int.MaxValue)]
                        int durationSeconds = 30,
                        [FromQuery]
                        string? egressProvider = null,
                        [FromQuery]
                        string? tags = null) =>
                            diagController.CaptureTrace(pid, uid, name, profile, durationSeconds, egressProvider, tags))
                        .WithName(nameof(DiagController.CaptureTrace))
                        .RequireHostRestriction()
                        .RequireAuthorization(AuthConstants.PolicyName)
                        .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest, ContentTypes.ApplicationProblemJson)
                        .Produces(StatusCodes.Status401Unauthorized)
                        .Produces<ProblemDetails>(StatusCodes.Status429TooManyRequests)
                        .Produces<FileResult>(StatusCodes.Status200OK, ContentTypes.ApplicationOctetStream)
                        .Produces(StatusCodes.Status202Accepted)
                        .ProducesProblem(StatusCodes.Status400BadRequest)
                        .RequireEgressValidation();

                    // CaptureTraceCustom
                    builder.MapGet("trace", (
                        [FromBody][Required]
                        EventPipeConfiguration configuration,
                        [FromQuery]
                        int? pid,
                        [FromQuery]
                        Guid? uid,
                        [FromQuery]
                        string? name,
                        [FromQuery][Range(-1, int.MaxValue)]
                        int durationSeconds = 30,
                        [FromQuery]
                        string? egressProvider = null,
                        [FromQuery]
                        string? tags = null) =>
                            diagController.CaptureTraceCustom(configuration, pid, uid, name, durationSeconds, egressProvider, tags))
                        .WithName(nameof(DiagController.CaptureTraceCustom))
                        .RequireHostRestriction()
                        .RequireAuthorization(AuthConstants.PolicyName)
                        .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest, ContentTypes.ApplicationProblemJson)
                        .Produces(StatusCodes.Status401Unauthorized)
                        .Produces<ProblemDetails>(StatusCodes.Status429TooManyRequests)
                        .Produces<FileResult>(StatusCodes.Status200OK, ContentTypes.ApplicationOctetStream)
                        .Produces(StatusCodes.Status202Accepted)
                        .ProducesProblem(StatusCodes.Status400BadRequest)
                        .RequireEgressValidation();

                    // CaptureLogs
                    builder.MapGet("logs", (
                        [FromQuery]
                        int? pid,
                        [FromQuery]
                        Guid? uid,
                        [FromQuery]
                        string? name,
                        [FromQuery][Range(-1, int.MaxValue)]
                        int durationSeconds = 30,
                        [FromQuery]
                        LogLevel? level = null,
                        [FromQuery]
                        string? egressProvider = null,
                        [FromQuery]
                        string? tags = null) =>
                            diagController.CaptureLogs(pid, uid, name, durationSeconds, level, egressProvider, tags))
                        .WithName(nameof(DiagController.CaptureLogs))
                        .RequireHostRestriction()
                        .RequireAuthorization(AuthConstants.PolicyName)
                        .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest, ContentTypes.ApplicationProblemJson)
                        .Produces(StatusCodes.Status401Unauthorized)
                        .Produces<ProblemDetails>(StatusCodes.Status429TooManyRequests)
                        .Produces<string>(StatusCodes.Status200OK, ContentTypes.ApplicationNdJson, ContentTypes.ApplicationJsonSequence, ContentTypes.TextPlain)
                        .Produces(StatusCodes.Status202Accepted)
                        .ProducesProblem(StatusCodes.Status400BadRequest)
                        .RequireEgressValidation();

                    // CaptureLogsCustom
                    builder.MapPost("logs", (
                        [FromBody]
                        LogsConfiguration configuration,
                        [FromQuery]
                        int? pid,
                        [FromQuery]
                        Guid? uid,
                        [FromQuery]
                        string? name,
                        [FromQuery][Range(-1, int.MaxValue)]
                        int durationSeconds = 30,
                        [FromQuery]
                        string? egressProvider = null,
                        [FromQuery]
                        string? tags = null) =>
                            diagController.CaptureLogsCustom(configuration, pid, uid, name, durationSeconds, egressProvider, tags))
                        .WithName(nameof(DiagController.CaptureLogs))
                        .RequireHostRestriction()
                        .RequireAuthorization(AuthConstants.PolicyName)
                        .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest, ContentTypes.ApplicationProblemJson)
                        .Produces(StatusCodes.Status401Unauthorized)
                        .Produces<ProblemDetails>(StatusCodes.Status429TooManyRequests)
                        .Produces<string>(StatusCodes.Status200OK, ContentTypes.ApplicationNdJson, ContentTypes.ApplicationJsonSequence, ContentTypes.TextPlain)
                        .Produces(StatusCodes.Status202Accepted)
                        .ProducesProblem(StatusCodes.Status400BadRequest)
                        .RequireEgressValidation();

                    // GetInfo
                    builder.MapGet("info", () => diagController.GetInfo())
                        .WithName(nameof(DiagController.GetInfo))
                        .RequireHostRestriction()
                        .RequireAuthorization(AuthConstants.PolicyName)
                        .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
                        .Produces(StatusCodes.Status401Unauthorized)
                        .Produces<DotnetMonitorInfo>(StatusCodes.Status200OK)
                        .ProducesProblem(StatusCodes.Status400BadRequest);

                    // GetCollectionRulesDescription
                    builder.MapGet("collectionrules", (
                        [FromQuery]
                        int pid,
                        [FromQuery]
                        Guid uid,
                        [FromQuery]
                        string name) =>
                            diagController.GetCollectionRulesDescription(pid, uid, name))
                        .WithName(nameof(DiagController.GetCollectionRulesDescription))
                        .RequireHostRestriction()
                        .RequireAuthorization(AuthConstants.PolicyName)
                        .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest, ContentTypes.ApplicationProblemJson)
                        .Produces(StatusCodes.Status401Unauthorized)
                        .Produces<Dictionary<string, CollectionRuleDescription>>(StatusCodes.Status200OK)
                        .ProducesProblem(StatusCodes.Status400BadRequest);

                    // GetCollectionRuleDetailedDescription
                    builder.MapGet("collectionrules/{collectionRuleName}", (
                        string collectionRuleName,
                        [FromQuery]
                        int pid,
                        [FromQuery]
                        Guid uid,
                        [FromQuery]
                        string name) =>
                            diagController.GetCollectionRuleDetailedDescription(collectionRuleName, pid, uid, name))
                        .WithName(nameof(DiagController.GetCollectionRuleDetailedDescription))
                        .RequireHostRestriction()
                        .RequireAuthorization(AuthConstants.PolicyName)
                        .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest, ContentTypes.ApplicationProblemJson)
                        .Produces(StatusCodes.Status401Unauthorized)
                        .Produces<CollectionRuleDetailedDescription>(StatusCodes.Status200OK)
                        .ProducesProblem(StatusCodes.Status400BadRequest);

                    // CaptureParameters
                    builder.MapPost("parameters", (
                        [FromBody][Required]
                        CaptureParametersConfiguration configuration,
                        [FromQuery][Range(-1, int.MaxValue)]
                        int durationSeconds = 30,
                        [FromQuery]
                        int? pid = null,
                        [FromQuery]
                        Guid? uid = null,
                        [FromQuery]
                        string? name = null,
                        [FromQuery]
                        string? egressProvider = null,
                        [FromQuery]
                        string? tags = null) =>
                            diagController.CaptureParameters(configuration, durationSeconds, pid, uid, name, egressProvider, tags))
                        .WithName(nameof(DiagController.CaptureParameters))
                        .RequireHostRestriction()
                        .RequireAuthorization(AuthConstants.PolicyName)
                        .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest, ContentTypes.ApplicationProblemJson)
                        .Produces(StatusCodes.Status401Unauthorized)
                        .Produces<ProblemDetails>(StatusCodes.Status429TooManyRequests)
                        .Produces<string>(StatusCodes.Status200OK, ContentTypes.ApplicationNdJson, ContentTypes.ApplicationJsonSequence, ContentTypes.TextPlain)
                        .Produces(StatusCodes.Status202Accepted)
                        .ProducesProblem(StatusCodes.Status400BadRequest)
                        .RequireEgressValidation();

                    // CaptureStacks
                    builder.MapGet("stacks", (
                        [FromQuery]
                        int? pid,
                        [FromQuery]
                        Guid? uid,
                        [FromQuery]
                        string? name,
                        [FromQuery]
                        string? egressProvider,
                        [FromQuery]
                        string? tags) =>
                            diagController.CaptureStacks(pid, uid, name, egressProvider, tags))
                        .WithName(nameof(DiagController.CaptureStacks))
                        .RequireHostRestriction()
                        .RequireAuthorization(AuthConstants.PolicyName)
                        .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest, ContentTypes.ApplicationProblemJson)
                        .Produces(StatusCodes.Status401Unauthorized)
                        .Produces<ProblemDetails>(StatusCodes.Status429TooManyRequests)
                        .Produces<string>(StatusCodes.Status200OK, ContentTypes.ApplicationJson, ContentTypes.TextPlain, ContentTypes.ApplicationSpeedscopeJson)
                        .Produces(StatusCodes.Status202Accepted)
                        .ProducesProblem(StatusCodes.Status400BadRequest)
                        .RequireEgressValidation();
                }

                // DiagController.Metrics
                {
                    // CaptureMetrics
                    builder.MapGet("livemetrics", (
                        [FromQuery]
                        int? pid,
                        [FromQuery]
                        Guid? uid,
                        [FromQuery]
                        string? name,
                        [FromQuery][Range(-1, int.MaxValue)]
                        int durationSeconds = 30,
                        [FromQuery]
                        string? egressProvider = null,
                        [FromQuery]
                        string? tags = null) =>
                            diagController.CaptureMetrics(pid, uid, name, durationSeconds, egressProvider, tags))
                        .WithName(nameof(DiagController.CaptureMetrics))
                        .RequireHostRestriction()
                        .RequireAuthorization(AuthConstants.PolicyName)
                        .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest, ContentTypes.ApplicationProblemJson)
                        .Produces(StatusCodes.Status401Unauthorized)
                        .Produces<ProblemDetails>(StatusCodes.Status429TooManyRequests)
                        .Produces<string>(StatusCodes.Status200OK, ContentTypes.ApplicationJsonSequence)
                        .Produces(StatusCodes.Status202Accepted)
                        .ProducesProblem(StatusCodes.Status400BadRequest)
                        .RequireEgressValidation();

                    builder.MapGet("livemetrics", (
                        [FromBody][Required]
                        Monitoring.WebApi.Models.EventMetricsConfiguration configuration,
                        [FromQuery]
                        int? pid,
                        [FromQuery]
                        Guid? uid,
                        [FromQuery]
                        string? name,
                        [FromQuery][Range(-1, int.MaxValue)]
                        int durationSeconds = 30,
                        [FromQuery]
                        string? egressProvider = null,
                        [FromQuery]
                        string? tags = null) =>
                            diagController.CaptureMetricsCustom(configuration, pid, uid, name, durationSeconds, egressProvider, tags))
                        .WithName(nameof(DiagController.CaptureMetricsCustom))
                        .RequireHostRestriction()
                        .RequireAuthorization(AuthConstants.PolicyName)
                        .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest, ContentTypes.ApplicationProblemJson)
                        .Produces(StatusCodes.Status401Unauthorized)
                        .Produces<ProblemDetails>(StatusCodes.Status429TooManyRequests)
                        .Produces<string>(StatusCodes.Status200OK, ContentTypes.ApplicationJsonSequence)
                        .Produces(StatusCodes.Status202Accepted)
                        .ProducesProblem(StatusCodes.Status400BadRequest)
                        .RequireEgressValidation();
                }

                // ExceptionsController
                {
                    var logger = serviceProvider.GetRequiredService<ILogger<ExceptionsController>>();
                    var exceptionsController = new ExceptionsController(serviceProvider, logger);

                    // GetExceptions
                    builder.MapGet("exceptions", (
                        [FromQuery]
                        int? pid,
                        [FromQuery]
                        Guid? uid,
                        [FromQuery]
                        string? name,
                        [FromQuery]
                        string? egressProvider = null,
                        [FromQuery]
                        string? tags = null) =>
                            exceptionsController.GetExceptions(pid, uid, name, egressProvider, tags))
                        .WithName(nameof(ExceptionsController.GetExceptions))
                        .RequireHostRestriction()
                        .RequireAuthorization(AuthConstants.PolicyName)
                        .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest, ContentTypes.ApplicationProblemJson)
                        .Produces<string>(StatusCodes.Status200OK, ContentTypes.ApplicationNdJson, ContentTypes.ApplicationJsonSequence, ContentTypes.TextPlain)
                        .Produces(StatusCodes.Status202Accepted)
                        .ProducesProblem(StatusCodes.Status400BadRequest)
                        .RequireEgressValidation();
                    
                    // CaptureExceptionsCustom
                    builder.MapPost("exceptions", (
                        [FromBody]
                        ExceptionsConfiguration configuration,
                        [FromQuery]
                        int? pid,
                        [FromQuery]
                        Guid? uid,
                        [FromQuery]
                        string? name,
                        [FromQuery]
                        string? egressProvider = null,
                        [FromQuery]
                        string? tags = null) =>
                            exceptionsController.CaptureExceptionsCustom(configuration, pid, uid, name, egressProvider, tags))
                        .WithName(nameof(ExceptionsController.CaptureExceptionsCustom))
                        .RequireHostRestriction()
                        .RequireAuthorization(AuthConstants.PolicyName)
                        .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest, ContentTypes.ApplicationProblemJson)
                        .Produces<string>(StatusCodes.Status200OK, ContentTypes.ApplicationNdJson, ContentTypes.ApplicationJsonSequence, ContentTypes.TextPlain)
                        .ProducesProblem(StatusCodes.Status400BadRequest)
                        .RequireEgressValidation();
                }

                // MetricsController
                {
                    var logger = serviceProvider.GetRequiredService<ILogger<MetricsController>>();
                    var metricsOptions = serviceProvider.GetRequiredService<IOptions<MetricsOptions>>();
                    var metricsController = new MetricsController(logger, serviceProvider, metricsOptions);

                    // GetMetrics
                    builder.MapGet("metrics", () => metricsController.GetMetrics())
                        .WithName(nameof(MetricsController.GetMetrics))
                        .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest, ContentTypes.ApplicationProblemJson)
                        .Produces<string>(StatusCodes.Status200OK, ContentTypes.TextPlain_v0_0_4)
                        .ProducesProblem(StatusCodes.Status400BadRequest);
                }

                builder.MapGet("/", (HttpResponse response, ISwaggerProvider provider) =>
                {
                    using Stream stream = response.BodyWriter.AsStream(true);

                    provider.WriteTo(stream);
                });

                app.UseMiddleware<EgressValidationUnhandledExceptionMiddleware>();
            });
        }
    }
}
