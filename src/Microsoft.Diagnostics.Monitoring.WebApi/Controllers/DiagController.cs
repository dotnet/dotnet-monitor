// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Diagnostics.Monitoring.WebApi.Validation;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Controllers
{
    static partial class RouteHandlerBuilderExtensions
    {
        public static RouteHandlerBuilder RequireDiagControllerCommon(this RouteHandlerBuilder builder)
        {
            return builder
                .RequireHostRestriction()
                .RequireAuthorization(AuthConstants.PolicyName)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest, ContentTypes.ApplicationProblemJson);
        }
    }

    public partial class DiagController : DiagnosticsControllerBase
    {
#pragma warning disable CA1823 // Avoid unused field warning since this is used as default parameter of lambda
        private const TraceProfile DefaultTraceProfiles = TraceProfile.Cpu | TraceProfile.Http | TraceProfile.Metrics | TraceProfile.GcCollect;
#pragma warning restore CA1823

        private readonly IOptions<DiagnosticPortOptions> _diagnosticPortOptions;
        private readonly IOptions<CallStacksOptions> _callStacksOptions;
        private readonly IOptions<ParameterCapturingOptions> _parameterCapturingOptions;
        private readonly IOptionsMonitor<GlobalCounterOptions> _counterOptions;
        private readonly IOptionsMonitor<MetricsOptions> _metricsOptions;
        private readonly ICollectionRuleService _collectionRuleService;
        private readonly IDumpOperationFactory _dumpOperationFactory;
        private readonly ILogsOperationFactory _logsOperationFactory;
        private readonly IMetricsOperationFactory _metricsOperationFactory;
        private readonly ITraceOperationFactory _traceOperationFactory;
        private readonly ICaptureParametersOperationFactory _captureParametersFactory;
        private readonly IGCDumpOperationFactory _gcdumpOperationFactory;
        private readonly IStacksOperationFactory _stacksOperationFactory;

        public DiagController(IServiceProvider serviceProvider, ILogger<DiagController> logger)
            : base(serviceProvider.GetRequiredService<IDiagnosticServices>(), serviceProvider.GetRequiredService<IEgressOperationStore>(), logger)
        {
            _diagnosticPortOptions = serviceProvider.GetRequiredService<IOptions<DiagnosticPortOptions>>();
            _callStacksOptions = serviceProvider.GetRequiredService<IOptions<CallStacksOptions>>();
            _parameterCapturingOptions = serviceProvider.GetRequiredService<IOptions<ParameterCapturingOptions>>();
            _counterOptions = serviceProvider.GetRequiredService<IOptionsMonitor<GlobalCounterOptions>>();
            _metricsOptions = serviceProvider.GetRequiredService<IOptionsMonitor<MetricsOptions>>();
            _collectionRuleService = serviceProvider.GetRequiredService<ICollectionRuleService>();
            _dumpOperationFactory = serviceProvider.GetRequiredService<IDumpOperationFactory>();
            _logsOperationFactory = serviceProvider.GetRequiredService<ILogsOperationFactory>();
            _metricsOperationFactory = serviceProvider.GetRequiredService<IMetricsOperationFactory>();
            _traceOperationFactory = serviceProvider.GetRequiredService<ITraceOperationFactory>();
            _captureParametersFactory = serviceProvider.GetRequiredService<ICaptureParametersOperationFactory>();
            _gcdumpOperationFactory = serviceProvider.GetRequiredService<IGCDumpOperationFactory>();
            _stacksOperationFactory = serviceProvider.GetRequiredService<IStacksOperationFactory>();
        }

        public static void MapActionMethods(IEndpointRouteBuilder builder)
        {
            // GetProcesses
            builder.MapGet("processes", (
                IServiceProvider serviceProvider,
                ILogger<DiagController> logger) =>
                    new DiagController(serviceProvider, logger).GetProcesses())
                .WithName(nameof(GetProcesses))
                .RequireDiagControllerCommon()
                .Produces<IEnumerable<ProcessIdentifier>>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest);

            // GetProcessInfo
            builder.MapGet("process", (
                IServiceProvider serviceProvider,
                ILogger<DiagController> logger,
                int? pid,
                Guid? uid,
                string? name) =>
                    new DiagController(serviceProvider, logger).GetProcessInfo(pid, uid, name))
                .WithName(nameof(GetProcessInfo))
                .RequireDiagControllerCommon()
                .Produces<Models.ProcessInfo>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest);

            // GetProcessEnvironment
            builder.MapGet("env", (
                IServiceProvider serviceProvider,
                ILogger<DiagController> logger,
                int? pid,
                Guid? uid,
                string? name) =>
                    new DiagController(serviceProvider, logger).GetProcessEnvironment(pid, uid, name))
                .WithName(nameof(GetProcessEnvironment))
                .RequireDiagControllerCommon()
                .Produces<Dictionary<string, string>>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest);

            // CaptureDump
            builder.MapGet("dump", (
                IServiceProvider serviceProvider,
                ILogger<DiagController> logger,
                int? pid,
                Guid? uid,
                string? name,
                Models.DumpType type = Models.DumpType.WithHeap,
                string? egressProvider = null,
                string? tags = null) =>
                    new DiagController(serviceProvider, logger).CaptureDump(pid, uid, name, type, egressProvider, tags))
                .WithName(nameof(CaptureDump))
                .RequireDiagControllerCommon()
                .Produces<ProblemDetails>(StatusCodes.Status429TooManyRequests)
                .Produces<FileResult>(StatusCodes.Status200OK, ContentTypes.ApplicationOctetStream)
                .Produces(StatusCodes.Status202Accepted)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .RequireEgressValidation();

            // CapturGcDump
            builder.MapGet("gcdump", (
                IServiceProvider serviceProvider,
                ILogger<DiagController> logger,
                int? pid,
                Guid? uid,
                string? name,
                string? egressProvider,
                string? tags) =>
                    new DiagController(serviceProvider, logger).CaptureGcDump(pid, uid, name, egressProvider, tags))
                .WithName(nameof(CaptureGcDump))
                .RequireDiagControllerCommon()
                .Produces<ProblemDetails>(StatusCodes.Status429TooManyRequests)
                .Produces<FileResult>(StatusCodes.Status200OK, ContentTypes.ApplicationOctetStream)
                .Produces(StatusCodes.Status202Accepted)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .RequireEgressValidation();

            // CaptureTrace
            builder.MapGet("trace", (
                IServiceProvider serviceProvider,
                ILogger<DiagController> logger,
                int? pid,
                Guid? uid,
                string? name,
                TraceProfile profile = DefaultTraceProfiles,
                [Range(-1, int.MaxValue)]
                int durationSeconds = 30,
                string? egressProvider = null,
                string? tags = null) =>
                    new DiagController(serviceProvider, logger).CaptureTrace(pid, uid, name, profile, durationSeconds, egressProvider, tags))
                .WithName(nameof(CaptureTrace))
                .RequireDiagControllerCommon()
                .Produces<ProblemDetails>(StatusCodes.Status429TooManyRequests)
                .Produces<FileResult>(StatusCodes.Status200OK, ContentTypes.ApplicationOctetStream)
                .Produces(StatusCodes.Status202Accepted)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .RequireEgressValidation();

            // CaptureTraceCustom
            builder.MapGet("trace", (
                IServiceProvider serviceProvider,
                ILogger<DiagController> logger,
                [FromBody][Required]
                EventPipeConfiguration configuration,
                int? pid,
                Guid? uid,
                string? name,
                [Range(-1, int.MaxValue)]
                int durationSeconds = 30,
                string? egressProvider = null,
                string? tags = null) =>
                    new DiagController(serviceProvider, logger).CaptureTraceCustom(configuration, pid, uid, name, durationSeconds, egressProvider, tags))
                .WithName(nameof(CaptureTraceCustom))
                .RequireDiagControllerCommon()
                .Produces<ProblemDetails>(StatusCodes.Status429TooManyRequests)
                .Produces<FileResult>(StatusCodes.Status200OK, ContentTypes.ApplicationOctetStream)
                .Produces(StatusCodes.Status202Accepted)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .RequireEgressValidation();

            // CaptureLogs
            builder.MapGet("logs", (
                IServiceProvider serviceProvider,
                ILogger<DiagController> logger,
                int? pid,
                Guid? uid,
                string? name,
                [Range(-1, int.MaxValue)]
                int durationSeconds = 30,
                LogLevel? level = null,
                string? egressProvider = null,
                string? tags = null) =>
                    new DiagController(serviceProvider, logger).CaptureLogs(pid, uid, name, durationSeconds, level, egressProvider, tags))
                .WithName(nameof(CaptureLogs))
                .RequireDiagControllerCommon()
                .Produces<ProblemDetails>(StatusCodes.Status429TooManyRequests)
                .Produces<string>(StatusCodes.Status200OK, ContentTypes.ApplicationNdJson, ContentTypes.ApplicationJsonSequence, ContentTypes.TextPlain)
                .Produces(StatusCodes.Status202Accepted)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .RequireEgressValidation();

            // CaptureLogsCustom
            builder.MapPost("logs", (
                IServiceProvider serviceProvider,
                ILogger<DiagController> logger,
                [FromBody]
                LogsConfiguration configuration,
                int? pid,
                Guid? uid,
                string? name,
                [Range(-1, int.MaxValue)]
                int durationSeconds = 30,
                string? egressProvider = null,
                string? tags = null) =>
                    new DiagController(serviceProvider, logger).CaptureLogsCustom(configuration, pid, uid, name, durationSeconds, egressProvider, tags))
                .WithName(nameof(CaptureLogs))
                .RequireDiagControllerCommon()
                .Produces<ProblemDetails>(StatusCodes.Status429TooManyRequests)
                .Produces<string>(StatusCodes.Status200OK, ContentTypes.ApplicationNdJson, ContentTypes.ApplicationJsonSequence, ContentTypes.TextPlain)
                .Produces(StatusCodes.Status202Accepted)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .RequireEgressValidation();

            // GetInfo
            builder.MapGet("info", (
                IServiceProvider serviceProvider,
                ILogger<DiagController> logger) =>
                    new DiagController(serviceProvider, logger).GetInfo())
                .WithName(nameof(GetInfo))
                .RequireDiagControllerCommon()
                .Produces<DotnetMonitorInfo>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest);

            // GetCollectionRulesDescription
            builder.MapGet("collectionrules", (
                IServiceProvider serviceProvider,
                ILogger<DiagController> logger,
                int pid,
                Guid uid,
                string name) =>
                    new DiagController(serviceProvider, logger).GetCollectionRulesDescription(pid, uid, name))
                .WithName(nameof(GetCollectionRulesDescription))
                .RequireDiagControllerCommon()
                .Produces<Dictionary<string, CollectionRuleDescription>>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest);

            // GetCollectionRuleDetailedDescription
            builder.MapGet("collectionrules/{collectionRuleName}", (
                IServiceProvider serviceProvider,
                ILogger<DiagController> logger,
                string collectionRuleName,
                int pid,
                Guid uid,
                string name) =>
                    new DiagController(serviceProvider, logger).GetCollectionRuleDetailedDescription(collectionRuleName, pid, uid, name))
                .WithName(nameof(GetCollectionRuleDetailedDescription))
                .RequireDiagControllerCommon()
                .Produces<CollectionRuleDetailedDescription>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest);

            // CaptureParameters
            builder.MapPost("parameters", (
                IServiceProvider serviceProvider,
                ILogger<DiagController> logger,
                [FromBody][Required]
                CaptureParametersConfiguration configuration,
                [Range(-1, int.MaxValue)]
                int durationSeconds = 30,
                int? pid = null,
                Guid? uid = null,
                string? name = null,
                string? egressProvider = null,
                string? tags = null) =>
                    new DiagController(serviceProvider, logger).CaptureParameters(configuration, durationSeconds, pid, uid, name, egressProvider, tags))
                .WithName(nameof(CaptureParameters))
                .RequireDiagControllerCommon()
                .Produces<ProblemDetails>(StatusCodes.Status429TooManyRequests)
                .Produces<string>(StatusCodes.Status200OK, ContentTypes.ApplicationNdJson, ContentTypes.ApplicationJsonSequence, ContentTypes.TextPlain)
                .Produces(StatusCodes.Status202Accepted)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .RequireEgressValidation();

            // CaptureStacks
            builder.MapGet("stacks", (
                IServiceProvider serviceProvider,
                ILogger<DiagController> logger,
                int? pid,
                Guid? uid,
                string? name,
                string? egressProvider,
                string? tags) =>
                    new DiagController(serviceProvider, logger).CaptureStacks(pid, uid, name, egressProvider, tags))
                .WithName(nameof(CaptureStacks))
                .RequireDiagControllerCommon()
                .Produces<ProblemDetails>(StatusCodes.Status429TooManyRequests)
                .Produces<string>(StatusCodes.Status200OK, ContentTypes.ApplicationJson, ContentTypes.TextPlain, ContentTypes.ApplicationSpeedscopeJson)
                .Produces(StatusCodes.Status202Accepted)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .RequireEgressValidation();
        }

        /// <summary>
        /// Get the list of accessible processes.
        /// </summary>
        public Task<IResult> GetProcesses()
        {
            return this.InvokeService(async () =>
            {
                IProcessInfo? defaultProcessInfo = null;
                try
                {
                    defaultProcessInfo = await DiagnosticServices.GetProcessAsync(null, HttpContext.RequestAborted);
                }
                catch (ArgumentException)
                {
                    // Unable to locate a default process; no action required
                }
                catch (InvalidOperationException)
                {
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    Logger.DefaultProcessUnexpectedFailure(ex);
                }

                IList<ProcessIdentifier> processesIdentifiers = new List<ProcessIdentifier>();
                foreach (IProcessInfo p in await DiagnosticServices.GetProcessesAsync(processFilter: null, HttpContext.RequestAborted))
                {
                    processesIdentifiers.Add(new ProcessIdentifier()
                    {
                        Pid = p.EndpointInfo.ProcessId,
                        Uid = p.EndpointInfo.RuntimeInstanceCookie,
                        Name = p.ProcessName,
                        IsDefault = (defaultProcessInfo != null &&
                            p.EndpointInfo.ProcessId == defaultProcessInfo.EndpointInfo.ProcessId &&
                            p.EndpointInfo.RuntimeInstanceCookie == defaultProcessInfo.EndpointInfo.RuntimeInstanceCookie)
                    });
                }
                Logger.WrittenToHttpStream();
                return TypedResults.Ok(processesIdentifiers);
            }, Logger);
        }

        /// <summary>
        /// Get information about the specified process.
        /// </summary>
        /// <param name="pid">Process ID used to identify the target process.</param>
        /// <param name="uid">The Runtime instance cookie used to identify the target process.</param>
        /// <param name="name">Process name used to identify the target process.</param>
        public Task<IResult> GetProcessInfo(
            int? pid,
            Guid? uid,
            string? name)
        {
            ProcessKey? processKey = Utilities.GetProcessKey(pid, uid, name);

            return InvokeForProcess(processInfo =>
            {
                Models.ProcessInfo processModel = new Models.ProcessInfo()
                {
                    CommandLine = processInfo.CommandLine,
                    Name = processInfo.ProcessName,
                    OperatingSystem = processInfo.OperatingSystem,
                    ProcessArchitecture = processInfo.ProcessArchitecture,
                    Pid = processInfo.EndpointInfo.ProcessId,
                    Uid = processInfo.EndpointInfo.RuntimeInstanceCookie
                };

                Logger.WrittenToHttpStream();

                return TypedResults.Ok(processModel);
            },
            processKey);
        }

        /// <summary>
        /// Get the environment block of the specified process.
        /// </summary>
        /// <param name="pid">Process ID used to identify the target process.</param>
        /// <param name="uid">The Runtime instance cookie used to identify the target process.</param>
        /// <param name="name">Process name used to identify the target process.</param>
        public Task<IResult> GetProcessEnvironment(
            int? pid,
            Guid? uid,
            string? name)
        {
            ProcessKey? processKey = Utilities.GetProcessKey(pid, uid, name);

            return InvokeForProcess<Ok<Dictionary<string, string>>>(async processInfo =>
            {
                var client = new DiagnosticsClient(processInfo.EndpointInfo.Endpoint);

                try
                {
                    Dictionary<string, string> environment = await client.GetProcessEnvironmentAsync(HttpContext.RequestAborted);

                    Logger.WrittenToHttpStream();

                    return TypedResults.Ok(environment);
                }
                catch (ServerErrorException)
                {
                    throw new InvalidOperationException(Strings.ErrorMessage_CanNotGetEnvironment);
                }
            },
            processKey);
        }

        /// <summary>
        /// Capture a dump of a process.
        /// </summary>
        /// <param name="pid">Process ID used to identify the target process.</param>
        /// <param name="uid">The Runtime instance cookie used to identify the target process.</param>
        /// <param name="name">Process name used to identify the target process.</param>
        /// <param name="type">The type of dump to capture.</param>
        /// <param name="egressProvider">The egress provider to which the dump is saved.</param>
        /// <param name="tags">An optional set of comma-separated identifiers users can include to make an operation easier to identify.</param>
        /// <returns></returns>
        // FileResult is the closest representation of the output so that the OpenAPI document correctly
        // describes the result as a binary file.
        public Task<IResult> CaptureDump(
            int? pid,
            Guid? uid,
            string? name,
            Models.DumpType type,
            string? egressProvider,
            string? tags)
        {
            ProcessKey? processKey = Utilities.GetProcessKey(pid, uid, name);

            return InvokeForProcess(
                processInfo => Result(
                    Utilities.ArtifactType_Dump,
                    egressProvider,
                    _dumpOperationFactory.Create(processInfo.EndpointInfo, type),
                    processInfo,
                    tags),
                processKey,
                Utilities.ArtifactType_Dump);
        }

        /// <summary>
        /// Capture a GC dump of a process.
        /// </summary>
        /// <param name="pid">Process ID used to identify the target process.</param>
        /// <param name="uid">The Runtime instance cookie used to identify the target process.</param>
        /// <param name="name">Process name used to identify the target process.</param>
        /// <param name="egressProvider">The egress provider to which the GC dump is saved.</param>
        /// <param name="tags">An optional set of comma-separated identifiers users can include to make an operation easier to identify.</param>
        /// <returns></returns>
        // FileResult is the closest representation of the output so that the OpenAPI document correctly
        // describes the result as a binary file.
        public Task<IResult> CaptureGcDump(
            int? pid,
            Guid? uid,
            string? name,
            string? egressProvider,
            string? tags)
        {
            ProcessKey? processKey = Utilities.GetProcessKey(pid, uid, name);

            return InvokeForProcess(
                processInfo => Result(
                    Utilities.ArtifactType_GCDump,
                    egressProvider,
                    _gcdumpOperationFactory.Create(processInfo.EndpointInfo),
                    processInfo,
                    tags),
                processKey,
                Utilities.ArtifactType_GCDump);
        }

        /// <summary>
        /// Capture a trace of a process.
        /// </summary>
        /// <param name="pid">Process ID used to identify the target process.</param>
        /// <param name="uid">The Runtime instance cookie used to identify the target process.</param>
        /// <param name="name">Process name used to identify the target process.</param>
        /// <param name="profile">The profiles enabled for the trace session.</param>
        /// <param name="durationSeconds">The duration of the trace session (in seconds).</param>
        /// <param name="egressProvider">The egress provider to which the trace is saved.</param>
        /// <param name="tags">An optional set of comma-separated identifiers users can include to make an operation easier to identify.</param>
        // FileResult is the closest representation of the output so that the OpenAPI document correctly
        // describes the result as a binary file.
        public Task<IResult> CaptureTrace(
            int? pid,
            Guid? uid,
            string? name,
            TraceProfile profile,
            int durationSeconds,
            string? egressProvider,
            string? tags)
        {
            ProcessKey? processKey = Utilities.GetProcessKey(pid, uid, name);

            return InvokeForProcess(processInfo =>
            {
                TimeSpan duration = Utilities.ConvertSecondsToTimeSpan(durationSeconds);

                var aggregateConfiguration = TraceUtilities.GetTraceConfiguration(profile, _counterOptions.CurrentValue);

                return StartTrace(processInfo, aggregateConfiguration, duration, egressProvider, tags);
            }, processKey, Utilities.ArtifactType_Trace);
        }

        /// <summary>
        /// Capture a trace of a process.
        /// </summary>
        /// <param name="configuration">The trace configuration describing which events to capture.</param>
        /// <param name="pid">Process ID used to identify the target process.</param>
        /// <param name="uid">The Runtime instance cookie used to identify the target process.</param>
        /// <param name="name">Process name used to identify the target process.</param>
        /// <param name="durationSeconds">The duration of the trace session (in seconds).</param>
        /// <param name="egressProvider">The egress provider to which the trace is saved.</param>
        /// <param name="tags">An optional set of comma-separated identifiers users can include to make an operation easier to identify.</param>
        // FileResult is the closest representation of the output so that the OpenAPI document correctly
        // describes the result as a binary file.
        public Task<IResult> CaptureTraceCustom(
            EventPipeConfiguration configuration,
            int? pid,
            Guid? uid,
            string? name,
            int durationSeconds,
            string? egressProvider,
            string? tags)
        {
            ProcessKey? processKey = Utilities.GetProcessKey(pid, uid, name);

            return InvokeForProcess(processInfo =>
            {
                foreach (Models.EventPipeProvider provider in configuration.Providers)
                {
                    if (!CounterValidator.ValidateProvider(_counterOptions.CurrentValue,
                        provider, out string? errorMessage))
                    {
                        throw new ValidationException(errorMessage);
                    }
                }

                TimeSpan duration = Utilities.ConvertSecondsToTimeSpan(durationSeconds);

                var traceConfiguration = TraceUtilities.GetTraceConfiguration(configuration.Providers, configuration.RequestRundown, configuration.BufferSizeInMB);

                return StartTrace(processInfo, traceConfiguration, duration, egressProvider, tags);
            }, processKey, Utilities.ArtifactType_Trace);
        }

        /// <summary>
        /// Capture a stream of logs from a process.
        /// </summary>
        /// <param name="pid">Process ID used to identify the target process.</param>
        /// <param name="uid">The Runtime instance cookie used to identify the target process.</param>
        /// <param name="name">Process name used to identify the target process.</param>
        /// <param name="durationSeconds">The duration of the logs session (in seconds).</param>
        /// <param name="level">The level of the logs to capture.</param>
        /// <param name="egressProvider">The egress provider to which the logs are saved.</param>
        /// <param name="tags">An optional set of comma-separated identifiers users can include to make an operation easier to identify.</param>
        public Task<IResult> CaptureLogs(
            int? pid,
            Guid? uid,
            string? name,
            int durationSeconds,
            LogLevel? level,
            string? egressProvider,
            string? tags)
        {
            ProcessKey? processKey = Utilities.GetProcessKey(pid, uid, name);

            return InvokeForProcess(processInfo =>
            {
                TimeSpan duration = Utilities.ConvertSecondsToTimeSpan(durationSeconds);

                var settings = new EventLogsPipelineSettings()
                {
                    Duration = duration
                };

                // Use log level query parameter if specified, otherwise use application-defined filters.
                if (level.HasValue)
                {
                    settings.LogLevel = level.Value;
                    settings.UseAppFilters = false;
                }
                else
                {
                    settings.UseAppFilters = true;
                }

                return StartLogs(processInfo, settings, egressProvider, tags);
            }, processKey, Utilities.ArtifactType_Logs);
        }

        /// <summary>
        /// Capture a stream of logs from a process.
        /// </summary>
        /// <param name="configuration">The logs configuration describing which logs to capture.</param>
        /// <param name="pid">Process ID used to identify the target process.</param>
        /// <param name="uid">The Runtime instance cookie used to identify the target process.</param>
        /// <param name="name">Process name used to identify the target process.</param>
        /// <param name="durationSeconds">The duration of the logs session (in seconds).</param>
        /// <param name="egressProvider">The egress provider to which the logs are saved.</param>
        /// <param name="tags">An optional set of comma-separated identifiers users can include to make an operation easier to identify.</param>
        public Task<IResult> CaptureLogsCustom(
            LogsConfiguration configuration,
            int? pid,
            Guid? uid,
            string? name,
            int durationSeconds,
            string? egressProvider,
            string? tags)
        {
            ProcessKey? processKey = Utilities.GetProcessKey(pid, uid, name);

            return InvokeForProcess(processInfo =>
            {
                TimeSpan duration = Utilities.ConvertSecondsToTimeSpan(durationSeconds);

                var settings = new EventLogsPipelineSettings()
                {
                    Duration = duration,
                    FilterSpecs = configuration.FilterSpecs,
                    LogLevel = configuration.LogLevel,
                    UseAppFilters = configuration.UseAppFilters
                };

                return StartLogs(processInfo, settings, egressProvider, tags);
            }, processKey, Utilities.ArtifactType_Logs);
        }

        /// <summary>
        /// Gets versioning and listening mode information about Dotnet-Monitor
        /// </summary>
        public IResult GetInfo()
        {
            return this.InvokeService(() =>
            {
                string? version = Assembly.GetExecutingAssembly().GetInformationalVersionString();
                string runtimeVersion = Environment.Version.ToString();
                DiagnosticPortConnectionMode diagnosticPortMode = _diagnosticPortOptions.Value.GetConnectionMode();
                string? diagnosticPortName = GetDiagnosticPortName();

                DotnetMonitorInfo dotnetMonitorInfo = new()
                {
                    Version = version,
                    RuntimeVersion = runtimeVersion,
                    DiagnosticPortMode = diagnosticPortMode,
                    DiagnosticPortName = diagnosticPortName
                };

                Logger.WrittenToHttpStream();
                return TypedResults.Ok(dotnetMonitorInfo);
            }, Logger);
        }

        /// <summary>
        /// Gets a brief summary about the current state of the collection rules.
        /// </summary>
        /// <param name="pid">Process ID used to identify the target process.</param>
        /// <param name="uid">The Runtime instance cookie used to identify the target process.</param>
        /// <param name="name">Process name used to identify the target process.</param>
        public Task<IResult> GetCollectionRulesDescription(
            int? pid,
            Guid? uid,
            string? name)
        {
            return InvokeForProcess<Ok<Dictionary<string, CollectionRuleDescription>>>(processInfo =>
            {
                return TypedResults.Ok(_collectionRuleService.GetCollectionRulesDescriptions(processInfo.EndpointInfo));
            },
            Utilities.GetProcessKey(pid, uid, name));
        }

        /// <summary>
        /// Gets detailed information about the current state of the specified collection rule.
        /// </summary>
        /// <param name="collectionRuleName">The name of the collection rule for which a detailed description should be provided.</param>
        /// <param name="pid">Process ID used to identify the target process.</param>
        /// <param name="uid">The Runtime instance cookie used to identify the target process.</param>
        /// <param name="name">Process name used to identify the target process.</param>
        public Task<IResult> GetCollectionRuleDetailedDescription(
            string collectionRuleName,
            int? pid,
            Guid? uid,
            string? name)
        {
            return InvokeForProcess<Ok<CollectionRuleDetailedDescription?>>(processInfo =>
            {
                return TypedResults.Ok<CollectionRuleDetailedDescription?>(_collectionRuleService.GetCollectionRuleDetailedDescription(collectionRuleName, processInfo.EndpointInfo));
            },
            Utilities.GetProcessKey(pid, uid, name));
        }

        public async Task<IResult> CaptureParameters(
            CaptureParametersConfiguration configuration,
            int durationSeconds,
            int? pid,
            Guid? uid,
            string? name,
            string? egressProvider,
            string? tags)
        {
            if (!_parameterCapturingOptions.Value.GetEnabled())
            {
                return this.FeatureNotEnabled(Strings.FeatureName_ParameterCapturing);
            }

            ProcessKey? processKey = Utilities.GetProcessKey(pid, uid, name);
            TimeSpan duration = Utilities.ConvertSecondsToTimeSpan(durationSeconds);

            return await InvokeForProcess(processInfo =>
            {
                CapturedParameterFormat format = ContentTypeUtilities.ComputeCapturedParameterFormat(Request.GetTypedHeaders().Accept) ?? CapturedParameterFormat.JsonSequence;

                IArtifactOperation operation = _captureParametersFactory.Create(processInfo.EndpointInfo, configuration, duration, format);

                return Result(
                    Utilities.ArtifactType_Parameters,
                    egressProvider,
                    operation,
                    processInfo,
                    tags,
                    format != CapturedParameterFormat.PlainText);
            }, processKey, Utilities.ArtifactType_Parameters);
        }

        public Task<IResult> CaptureStacks(
            int? pid,
            Guid? uid,
            string? name,
            string? egressProvider,
            string? tags)
        {
            if (!_callStacksOptions.Value.GetEnabled())
            {
                return Task.FromResult<IResult>(this.FeatureNotEnabled(Strings.FeatureName_CallStacks));
            }

            ProcessKey? processKey = Utilities.GetProcessKey(pid, uid, name);

            return InvokeForProcess(processInfo =>
            {
                //Stack format based on Content-Type

                StackFormat stackFormat = ContentTypeUtilities.ComputeStackFormat(Request.GetTypedHeaders().Accept) ?? StackFormat.PlainText;

                IArtifactOperation operation = _stacksOperationFactory.Create(processInfo.EndpointInfo, stackFormat);

                return Result(
                    Utilities.ArtifactType_Stacks,
                    egressProvider,
                    operation,
                    processInfo,
                    tags);
            }, processKey, Utilities.ArtifactType_Stacks);
        }

        private string? GetDiagnosticPortName()
        {
            return _diagnosticPortOptions.Value.EndpointName;
        }

        private Task<IResult> StartTrace(
            IProcessInfo processInfo,
            MonitoringSourceConfiguration configuration,
            TimeSpan duration,
            string? egressProvider,
            string? tags)
        {
            IArtifactOperation traceOperation = _traceOperationFactory.Create(
                processInfo.EndpointInfo,
                configuration,
                duration);

            return Result(
                Utilities.ArtifactType_Trace,
                egressProvider,
                traceOperation,
                processInfo,
                tags);
        }

        private Task<IResult> StartLogs(
            IProcessInfo processInfo,
            EventLogsPipelineSettings settings,
            string? egressProvider,
            string? tags)
        {
            LogFormat? format = ComputeLogFormat(Request.GetTypedHeaders().Accept);
            if (null == format)
            {
                return Task.FromResult(this.NotAcceptable());
            }

            // Allow sync I/O on logging routes due to StreamLogger's usage.
            HttpContext.AllowSynchronousIO();

            IArtifactOperation logsOperation = _logsOperationFactory.Create(
                processInfo.EndpointInfo,
                settings,
                format.Value);

            return Result(
                Utilities.ArtifactType_Logs,
                egressProvider,
                logsOperation,
                processInfo,
                tags,
                format != LogFormat.PlainText);
        }

        private static LogFormat? ComputeLogFormat(IList<MediaTypeHeaderValue> acceptedHeaders)
        {
            if (acceptedHeaders == null || acceptedHeaders.Count == 0)
            {
                return null;
            }

            if (acceptedHeaders.Contains(ContentTypeUtilities.TextPlainHeader))
            {
                return LogFormat.PlainText;
            }
            if (acceptedHeaders.Contains(ContentTypeUtilities.NdJsonHeader))
            {
                return LogFormat.NewlineDelimitedJson;
            }
            if (acceptedHeaders.Contains(ContentTypeUtilities.JsonSequenceHeader))
            {
                return LogFormat.JsonSequence;
            }
            if (acceptedHeaders.Any(ContentTypeUtilities.TextPlainHeader.IsSubsetOf))
            {
                return LogFormat.PlainText;
            }
            if (acceptedHeaders.Any(ContentTypeUtilities.NdJsonHeader.IsSubsetOf))
            {
                return LogFormat.NewlineDelimitedJson;
            }
            if (acceptedHeaders.Any(ContentTypeUtilities.JsonSequenceHeader.IsSubsetOf))
            {
                return LogFormat.JsonSequence;
            }
            return null;
        }
    }
}
