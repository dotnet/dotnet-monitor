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
using System.ComponentModel;
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
                .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest, ContentTypes.ApplicationProblemJson)
                .WithTags("Diag");
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
        private readonly IEnumerable<IMonitorCapability> _monitorCapabilities;
        public DiagController(HttpContext httpContext, ILogger<DiagController> logger) :
            base(httpContext, httpContext.RequestServices, logger)
        {
            var serviceProvider = httpContext.RequestServices;
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
            _monitorCapabilities = serviceProvider.GetRequiredService<IEnumerable<IMonitorCapability>>();
        }

        public static void MapActionMethods(IEndpointRouteBuilder builder)
        {
            // GetProcesses
            builder.MapGet("processes",
                [EndpointSummary("Get the list of accessible processes.")] (
                HttpContext context,
                ILogger<DiagController> logger) =>
                    new DiagController(context, logger).GetProcesses())
                .WithName(nameof(GetProcesses))
                .RequireDiagControllerCommon()
                .Produces<IEnumerable<ProcessIdentifier>>(StatusCodes.Status200OK);

            // GetProcessInfo
            builder.MapGet("process",
                [EndpointSummary("Get information about the specified process.")] (
                HttpContext context,
                ILogger<DiagController> logger,
                [FromQuery, Description("Process ID used to identify the target process.")] int? pid,
                [FromQuery, Description("The Runtime instance cookie used to identify the target process.")] Guid? uid,
                [FromQuery, Description("Process name used to identify the target process.")] string? name) =>
                    new DiagController(context, logger).GetProcessInfo(pid, uid, name))
                .WithName(nameof(GetProcessInfo))
                .RequireDiagControllerCommon()
                .Produces<Models.ProcessInfo>(StatusCodes.Status200OK);

            // GetProcessEnvironment
            builder.MapGet("env",
                [EndpointSummary("Get the environment block of the specified process.")] (
                HttpContext context,
                ILogger<DiagController> logger,
                [FromQuery, Description("Process ID used to identify the target process.")] int? pid,
                [FromQuery, Description("The Runtime instance cookie used to identify the target process.")] Guid? uid,
                [FromQuery, Description("Process name used to identify the target process.")] string? name) =>
                    new DiagController(context, logger).GetProcessEnvironment(pid, uid, name))
                .WithName(nameof(GetProcessEnvironment))
                .RequireDiagControllerCommon()
                .Produces<Dictionary<string, string>>(StatusCodes.Status200OK);

            // CaptureDump
            builder.MapGet("dump",
                [EndpointSummary("Capture a dump of a process.")] (
                HttpContext context,
                ILogger<DiagController> logger,
                [FromQuery, Description("Process ID used to identify the target process.")] int? pid,
                [FromQuery, Description("The Runtime instance cookie used to identify the target process.")] Guid? uid,
                [FromQuery, Description("Process name used to identify the target process.")] string? name,
                [FromQuery, Description("The type of dump to capture.")] Models.DumpType type = Models.DumpType.WithHeap,
                [FromQuery, Description("The egress provider to which the dump is saved.")] string? egressProvider = null,
                [FromQuery, Description("An optional set of comma-separated identifiers users can include to make an operation easier to identify.")] string? tags = null) =>
                    new DiagController(context, logger).CaptureDump(pid, uid, name, type, egressProvider, tags))
                .WithName(nameof(CaptureDump))
                .RequireDiagControllerCommon()
                .Produces<ProblemDetails>(StatusCodes.Status429TooManyRequests)
                .Produces<FileResult>(StatusCodes.Status200OK, ContentTypes.ApplicationOctetStream)
                // .Produces(StatusCodes.Status200OK, ContentTypes.ApplicationOctetStream)
                // .Produces<FileStreamHttpResult>(StatusCodes.Status200OK, ContentTypes.ApplicationOctetStream)
                // .Produces<FileStreamHttpResult>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status202Accepted)
                .RequireEgressValidation();

            // CaptureGcDump
            builder.MapGet("gcdump",
                [EndpointSummary("Capture a GC dump of a process.")] (
                HttpContext context,
                ILogger<DiagController> logger,
                [FromQuery, Description("Process ID used to identify the target process.")] int? pid,
                [FromQuery, Description("The Runtime instance cookie used to identify the target process.")] Guid? uid,
                [FromQuery, Description("Process name used to identify the target process.")] string? name,
                [FromQuery, Description("The egress provider to which the GC dump is saved.")] string? egressProvider,
                [FromQuery, Description("An optional set of comma-separated identifiers users can include to make an operation easier to identify.")] string? tags) =>
                    new DiagController(context, logger).CaptureGcDump(pid, uid, name, egressProvider, tags))
                .WithName(nameof(CaptureGcDump))
                .RequireDiagControllerCommon()
                .Produces<ProblemDetails>(StatusCodes.Status429TooManyRequests)
                .Produces<FileResult>(StatusCodes.Status200OK, ContentTypes.ApplicationOctetStream)
                .Produces(StatusCodes.Status202Accepted)
                .RequireEgressValidation();

            // CaptureTrace
            builder.MapGet("trace",
                [EndpointSummary("Capture a trace of a process.")] (
                HttpContext context,
                ILogger<DiagController> logger,
                [FromQuery, Description("Process ID used to identify the target process.")] int? pid,
                [FromQuery, Description("The Runtime instance cookie used to identify the target process.")] Guid? uid,
                [FromQuery, Description("Process name used to identify the target process.")] string? name,
                [FromQuery, Description("The profiles enabled for the trace session.")] TraceProfile profile = DefaultTraceProfiles,
                [FromQuery, Description("The duration of the trace session (in seconds)."), Range(-1, int.MaxValue)] int durationSeconds = 30,
                [FromQuery, Description("The egress provider to which the trace is saved.")] string? egressProvider = null,
                [FromQuery, Description("An optional set of comma-separated identifiers users can include to make an operation easier to identify.")] string? tags = null) =>
                    new DiagController(context, logger).CaptureTrace(pid, uid, name, profile, durationSeconds, egressProvider, tags))
                .WithName(nameof(CaptureTrace))
                .RequireDiagControllerCommon()
                .Produces<ProblemDetails>(StatusCodes.Status429TooManyRequests)
                .Produces<FileResult>(StatusCodes.Status200OK, ContentTypes.ApplicationOctetStream)
                .Produces(StatusCodes.Status202Accepted)
                .RequireEgressValidation();

            // CaptureTraceCustom
            builder.MapPost("trace",
                [EndpointSummary("Capture a trace of a process.")] (
                HttpContext context,
                ILogger<DiagController> logger,
                [FromBody, Required, Description("The trace configuration describing which events to capture.")] EventPipeConfiguration configuration,
                [FromQuery, Description("Process ID used to identify the target process.")] int? pid,
                [FromQuery, Description("The Runtime instance cookie used to identify the target process.")] Guid? uid,
                [FromQuery, Description("Process name used to identify the target process.")] string? name,
                [FromQuery, Description("The duration of the trace session (in seconds)."), Range(-1, int.MaxValue)] int durationSeconds = 30,
                [FromQuery, Description("The egress provider to which the trace is saved.")] string? egressProvider = null,
                [FromQuery, Description("An optional set of comma-separated identifiers users can include to make an operation easier to identify.")] string? tags = null) =>
                    new DiagController(context, logger).CaptureTraceCustom(configuration, pid, uid, name, durationSeconds, egressProvider, tags))
                .WithName(nameof(CaptureTraceCustom))
                .RequireDiagControllerCommon()
                .Produces<ProblemDetails>(StatusCodes.Status429TooManyRequests)
                .Produces<FileResult>(StatusCodes.Status200OK, ContentTypes.ApplicationOctetStream)
                .Produces(StatusCodes.Status202Accepted)
                // TODO: does it actually accept these?
                .Accepts<EventPipeConfiguration>(ContentTypes.ApplicationJson, ContentTypes.TextJson, ContentTypes.ApplicationAnyJson)
                .RequireEgressValidation();

            // CaptureLogs
            builder.MapGet("logs",
                [EndpointSummary("Capture a stream of logs from a process.")] (
                HttpContext context,
                ILogger<DiagController> logger,
                [FromQuery, Description("Process ID used to identify the target process.")] int? pid,
                [FromQuery, Description("The Runtime instance cookie used to identify the target process.")] Guid? uid,
                [FromQuery, Description("Process name used to identify the target process.")] string? name,
                [FromQuery, Description("The duration of the logs session (in seconds)."), Range(-1, int.MaxValue)] int durationSeconds = 30,
                [FromQuery, Description("The level of the logs to capture.")] LogLevel? level = null,
                [FromQuery, Description("The egress provider to which the logs are saved.")] string? egressProvider = null,
                [FromQuery, Description("An optional set of comma-separated identifiers users can include to make an operation easier to identify.")] string? tags = null) =>
                    new DiagController(context, logger).CaptureLogs(pid, uid, name, durationSeconds, level, egressProvider, tags))
                .WithName(nameof(CaptureLogs))
                .RequireDiagControllerCommon()
                .Produces<ProblemDetails>(StatusCodes.Status429TooManyRequests)
                .Produces<string>(StatusCodes.Status200OK, ContentTypes.ApplicationNdJson, ContentTypes.ApplicationJsonSequence, ContentTypes.TextPlain)
                .Produces(StatusCodes.Status202Accepted)
                .RequireEgressValidation();

            // CaptureLogsCustom
            builder.MapPost("logs",
                [EndpointSummary("Capture a stream of logs from a process.")] (
                HttpContext context,
                ILogger<DiagController> logger,
                [FromBody, Description("The logs configuration describing which logs to capture.")] LogsConfiguration configuration,
                [FromQuery, Description("Process ID used to identify the target process.")] int? pid,
                [FromQuery, Description("The Runtime instance cookie used to identify the target process.")] Guid? uid,
                [FromQuery, Description("Process name used to identify the target process.")] string? name,
                [FromQuery, Description("The duration of the logs session (in seconds)."), Range(-1, int.MaxValue)] int durationSeconds = 30,
                [FromQuery, Description("The egress provider to which the logs are saved.")] string? egressProvider = null,
                [FromQuery, Description("An optional set of comma-separated identifiers users can include to make an operation easier to identify.")] string? tags = null) =>
                    new DiagController(context, logger).CaptureLogsCustom(configuration, pid, uid, name, durationSeconds, egressProvider, tags))
                .WithName(nameof(CaptureLogsCustom))
                .RequireDiagControllerCommon()
                .Produces<ProblemDetails>(StatusCodes.Status429TooManyRequests)
                .Produces<string>(StatusCodes.Status200OK, ContentTypes.ApplicationNdJson, ContentTypes.ApplicationJsonSequence, ContentTypes.TextPlain)
                .Produces(StatusCodes.Status202Accepted)
                .Accepts<LogsConfiguration>(ContentTypes.ApplicationJson, ContentTypes.TextJson, ContentTypes.ApplicationAnyJson)
                .RequireEgressValidation();

            // GetInfo
            builder.MapGet("info",
                [EndpointSummary("Gets versioning and listening mode information about Dotnet-Monitor")] (
                HttpContext context,
                ILogger<DiagController> logger) =>
                    new DiagController(context, logger).GetInfo())
                .WithName(nameof(GetInfo))
                .RequireDiagControllerCommon()
                .Produces<DotnetMonitorInfo>(StatusCodes.Status200OK);

            // GetCollectionRulesDescription
            builder.MapGet("collectionrules",
                [EndpointSummary("Gets a brief summary about the current state of the collection rules.")] (
                HttpContext context,
                ILogger<DiagController> logger,
                [FromQuery, Description("Process ID used to identify the target process.")] int? pid,
                [FromQuery, Description("The Runtime instance cookie used to identify the target process.")] Guid? uid,
                [FromQuery, Description("Process name used to identify the target process.")] string? name) =>
                    new DiagController(context, logger).GetCollectionRulesDescription(pid, uid, name))
                .WithName(nameof(GetCollectionRulesDescription))
                .RequireDiagControllerCommon()
                .Produces<Dictionary<string, CollectionRuleDescription>>(StatusCodes.Status200OK);

            // GetCollectionRuleDetailedDescription
            builder.MapGet("collectionrules/{collectionRuleName}",
                [EndpointSummary("Gets detailed information about the current state of the specified collection rule.")] (
                HttpContext context,
                ILogger<DiagController> logger,
                [FromRoute, Description("The name of the collection rule for which a detailed description should be provided.")] string collectionRuleName,
                [FromQuery, Description("Process ID used to identify the target process.")] int? pid,
                [FromQuery, Description("The Runtime instance cookie used to identify the target process.")] Guid? uid,
                [FromQuery, Description("Process name used to identify the target process.")] string? name) =>
                    new DiagController(context, logger).GetCollectionRuleDetailedDescription(collectionRuleName, pid, uid, name))
                .WithName(nameof(GetCollectionRuleDetailedDescription))
                .RequireDiagControllerCommon()
                .Produces<CollectionRuleDetailedDescription>(StatusCodes.Status200OK);

            // CaptureParameters
            builder.MapPost("parameters", (
                HttpContext context,
                ILogger<DiagController> logger,
                [FromBody, Required] CaptureParametersConfiguration configuration,
                [FromQuery, Range(-1, int.MaxValue)] int durationSeconds = 30,
                [FromQuery] int? pid = null,
                [FromQuery] Guid? uid = null,
                [FromQuery] string? name = null,
                [FromQuery] string? egressProvider = null,
                [FromQuery] string? tags = null) =>
                    new DiagController(context, logger).CaptureParameters(configuration, durationSeconds, pid, uid, name, egressProvider, tags))
                .WithName(nameof(CaptureParameters))
                .RequireDiagControllerCommon()
                .Produces<ProblemDetails>(StatusCodes.Status429TooManyRequests)
                .Produces<string>(StatusCodes.Status200OK, ContentTypes.ApplicationNdJson, ContentTypes.ApplicationJsonSequence, ContentTypes.TextPlain)
                .Produces(StatusCodes.Status202Accepted)
                .Accepts<CaptureParametersConfiguration>(ContentTypes.ApplicationJson, ContentTypes.TextJson, ContentTypes.ApplicationAnyJson)
                .RequireEgressValidation();

            // CaptureStacks
            builder.MapGet("stacks", (
                HttpContext context,
                ILogger<DiagController> logger,
                [FromQuery] int? pid,
                [FromQuery] Guid? uid,
                [FromQuery] string? name,
                [FromQuery] string? egressProvider,
                [FromQuery] string? tags) =>
                    new DiagController(context, logger).CaptureStacks(pid, uid, name, egressProvider, tags))
                .WithName(nameof(CaptureStacks))
                .RequireDiagControllerCommon()
                .Produces<ProblemDetails>(StatusCodes.Status429TooManyRequests)
                .Produces<string>(StatusCodes.Status200OK, ContentTypes.ApplicationJson, ContentTypes.TextPlain, ContentTypes.ApplicationSpeedscopeJson)
                .Produces(StatusCodes.Status202Accepted)
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
                    ManagedEntryPointAssemblyName = processInfo.ManagedEntryPointAssemblyName,
                    Pid = processInfo.EndpointInfo.ProcessId,
                    Uid = processInfo.EndpointInfo.RuntimeInstanceCookie
                };

                Logger.WrittenToHttpStream();

                return TypedResults.Ok(processModel);
            },
            processKey);
        }

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
                    DiagnosticPortName = diagnosticPortName,
                    Capabilities = _monitorCapabilities.Select(c => new MonitorCapability(c.Name, c.Enabled)).ToArray()
                };

                Logger.WrittenToHttpStream();
                return TypedResults.Ok(dotnetMonitorInfo);
            }, Logger);
        }

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
