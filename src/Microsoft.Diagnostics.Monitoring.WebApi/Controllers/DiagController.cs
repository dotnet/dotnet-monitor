// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
    [Route("")] // Root
    [ApiController]
    [HostRestriction]
    [Authorize(Policy = AuthConstants.PolicyName)]
#if NETCOREAPP3_1_OR_GREATER
    [ProducesErrorResponseType(typeof(ValidationProblemDetails))]
#endif
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
    public partial class DiagController : DiagnosticsControllerBase
    {
        private const TraceProfile DefaultTraceProfiles = TraceProfile.Cpu | TraceProfile.Http | TraceProfile.Metrics | TraceProfile.GcCollect;

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

        /// <summary>
        /// Get the list of accessible processes.
        /// </summary>
        [HttpGet("processes", Name = nameof(GetProcesses))]
        [ProducesWithProblemDetails(ContentTypes.ApplicationJson)]
        [ProducesResponseType(typeof(IEnumerable<ProcessIdentifier>), StatusCodes.Status200OK)]
        public Task<ActionResult<IEnumerable<ProcessIdentifier>>> GetProcesses()
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
                return new ActionResult<IEnumerable<ProcessIdentifier>>(processesIdentifiers);
            }, Logger);
        }

        /// <summary>
        /// Get information about the specified process.
        /// </summary>
        /// <param name="pid">Process ID used to identify the target process.</param>
        /// <param name="uid">The Runtime instance cookie used to identify the target process.</param>
        /// <param name="name">Process name used to identify the target process.</param>
        [HttpGet("process", Name = nameof(GetProcessInfo))]
        [ProducesWithProblemDetails(ContentTypes.ApplicationJson)]
        [ProducesResponseType(typeof(Models.ProcessInfo), StatusCodes.Status200OK)]
        public Task<ActionResult<Models.ProcessInfo>> GetProcessInfo(
            [FromQuery]
            int? pid = null,
            [FromQuery]
            Guid? uid = null,
            [FromQuery]
            string? name = null)
        {
            ProcessKey? processKey = Utilities.GetProcessKey(pid, uid, name);

            return InvokeForProcess<Models.ProcessInfo>(processInfo =>
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

                return processModel;
            },
            processKey);
        }

        /// <summary>
        /// Get the environment block of the specified process.
        /// </summary>
        /// <param name="pid">Process ID used to identify the target process.</param>
        /// <param name="uid">The Runtime instance cookie used to identify the target process.</param>
        /// <param name="name">Process name used to identify the target process.</param>
        [HttpGet("env", Name = nameof(GetProcessEnvironment))]
        [ProducesWithProblemDetails(ContentTypes.ApplicationJson)]
        [ProducesResponseType(typeof(Dictionary<string, string>), StatusCodes.Status200OK)]
        public Task<ActionResult<Dictionary<string, string>>> GetProcessEnvironment(
            [FromQuery]
            int? pid = null,
            [FromQuery]
            Guid? uid = null,
            [FromQuery]
            string? name = null)
        {
            ProcessKey? processKey = Utilities.GetProcessKey(pid, uid, name);

            return InvokeForProcess<Dictionary<string, string>>(async processInfo =>
            {
                var client = new DiagnosticsClient(processInfo.EndpointInfo.Endpoint);

                try
                {
                    Dictionary<string, string> environment = await client.GetProcessEnvironmentAsync(HttpContext.RequestAborted);

                    Logger.WrittenToHttpStream();

                    return environment;
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
        [HttpGet("dump", Name = nameof(CaptureDump))]
        [ProducesWithProblemDetails(ContentTypes.ApplicationOctetStream)]
        // FileResult is the closest representation of the output so that the OpenAPI document correctly
        // describes the result as a binary file.
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status202Accepted)]
        [EgressValidation]
        public Task<ActionResult> CaptureDump(
            [FromQuery]
            int? pid = null,
            [FromQuery]
            Guid? uid = null,
            [FromQuery]
            string? name = null,
            [FromQuery]
            Models.DumpType type = Models.DumpType.WithHeap,
            [FromQuery]
            string? egressProvider = null,
            [FromQuery]
            string? tags = null)
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
        [HttpGet("gcdump", Name = nameof(CaptureGcDump))]
        [ProducesWithProblemDetails(ContentTypes.ApplicationOctetStream)]
        // FileResult is the closest representation of the output so that the OpenAPI document correctly
        // describes the result as a binary file.
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status202Accepted)]
        [EgressValidation]
        public Task<ActionResult> CaptureGcDump(
            [FromQuery]
            int? pid = null,
            [FromQuery]
            Guid? uid = null,
            [FromQuery]
            string? name = null,
            [FromQuery]
            string? egressProvider = null,
            [FromQuery]
            string? tags = null)
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
        [HttpGet("trace", Name = nameof(CaptureTrace))]
        [ProducesWithProblemDetails(ContentTypes.ApplicationOctetStream)]
        // FileResult is the closest representation of the output so that the OpenAPI document correctly
        // describes the result as a binary file.
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status202Accepted)]
        [EgressValidation]
        public Task<ActionResult> CaptureTrace(
            [FromQuery]
            int? pid = null,
            [FromQuery]
            Guid? uid = null,
            [FromQuery]
            string? name = null,
            [FromQuery]
            TraceProfile profile = DefaultTraceProfiles,
            [FromQuery][Range(-1, int.MaxValue)]
            int durationSeconds = 30,
            [FromQuery]
            string? egressProvider = null,
            [FromQuery]
            string? tags = null)
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
        [HttpPost("trace", Name = nameof(CaptureTraceCustom))]
        [ProducesWithProblemDetails(ContentTypes.ApplicationOctetStream)]
        // FileResult is the closest representation of the output so that the OpenAPI document correctly
        // describes the result as a binary file.
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status202Accepted)]
        [EgressValidation]
        public Task<ActionResult> CaptureTraceCustom(
            [FromBody][Required]
            EventPipeConfiguration configuration,
            [FromQuery]
            int? pid = null,
            [FromQuery]
            Guid? uid = null,
            [FromQuery]
            string? name = null,
            [FromQuery][Range(-1, int.MaxValue)]
            int durationSeconds = 30,
            [FromQuery]
            string? egressProvider = null,
            [FromQuery]
            string? tags = null)
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
        [HttpGet("logs", Name = nameof(CaptureLogs))]
        [ProducesWithProblemDetails(ContentTypes.ApplicationNdJson, ContentTypes.ApplicationJsonSequence, ContentTypes.TextPlain)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status202Accepted)]
        [EgressValidation]
        public Task<ActionResult> CaptureLogs(
            [FromQuery]
            int? pid = null,
            [FromQuery]
            Guid? uid = null,
            [FromQuery]
            string? name = null,
            [FromQuery][Range(-1, int.MaxValue)]
            int durationSeconds = 30,
            [FromQuery]
            LogLevel? level = null,
            [FromQuery]
            string? egressProvider = null,
            [FromQuery]
            string? tags = null)
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
        [HttpPost("logs", Name = nameof(CaptureLogsCustom))]
        [ProducesWithProblemDetails(ContentTypes.ApplicationNdJson, ContentTypes.ApplicationJsonSequence, ContentTypes.TextPlain)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status202Accepted)]
        [EgressValidation]
        public Task<ActionResult> CaptureLogsCustom(
            [FromBody]
            LogsConfiguration configuration,
            [FromQuery]
            int? pid = null,
            [FromQuery]
            Guid? uid = null,
            [FromQuery]
            string? name = null,
            [FromQuery][Range(-1, int.MaxValue)]
            int durationSeconds = 30,
            [FromQuery]
            string? egressProvider = null,
            [FromQuery]
            string? tags = null)
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
        [HttpGet("info", Name = nameof(GetInfo))]
        [ProducesWithProblemDetails(ContentTypes.ApplicationJson)]
        [ProducesResponseType(typeof(DotnetMonitorInfo), StatusCodes.Status200OK)]
        public ActionResult<DotnetMonitorInfo> GetInfo()
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
                return new ActionResult<DotnetMonitorInfo>(dotnetMonitorInfo);
            }, Logger);
        }

        /// <summary>
        /// Gets a brief summary about the current state of the collection rules.
        /// </summary>
        /// <param name="pid">Process ID used to identify the target process.</param>
        /// <param name="uid">The Runtime instance cookie used to identify the target process.</param>
        /// <param name="name">Process name used to identify the target process.</param>
        [HttpGet("collectionrules", Name = nameof(GetCollectionRulesDescription))]
        [ProducesWithProblemDetails(ContentTypes.ApplicationJson)]
        [ProducesResponseType(typeof(Dictionary<string, CollectionRuleDescription>), StatusCodes.Status200OK)]
        public Task<ActionResult<Dictionary<string, CollectionRuleDescription>>> GetCollectionRulesDescription(
            [FromQuery]
            int? pid = null,
            [FromQuery]
            Guid? uid = null,
            [FromQuery]
            string? name = null)
        {
            return InvokeForProcess<Dictionary<string, CollectionRuleDescription>>(processInfo =>
            {
                return _collectionRuleService.GetCollectionRulesDescriptions(processInfo.EndpointInfo);
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
        [HttpGet("collectionrules/{collectionRuleName}", Name = nameof(GetCollectionRuleDetailedDescription))]
        [ProducesWithProblemDetails(ContentTypes.ApplicationJson)]
        [ProducesResponseType(typeof(CollectionRuleDetailedDescription), StatusCodes.Status200OK)]
        public Task<ActionResult<CollectionRuleDetailedDescription?>> GetCollectionRuleDetailedDescription(
            string collectionRuleName,
            [FromQuery]
            int? pid = null,
            [FromQuery]
            Guid? uid = null,
            [FromQuery]
            string? name = null)
        {
            return InvokeForProcess<CollectionRuleDetailedDescription?>(processInfo =>
            {
                return _collectionRuleService.GetCollectionRuleDetailedDescription(collectionRuleName, processInfo.EndpointInfo);
            },
            Utilities.GetProcessKey(pid, uid, name));
        }

        [HttpPost("parameters", Name = nameof(CaptureParameters))]
        [ProducesWithProblemDetails(ContentTypes.ApplicationNdJson, ContentTypes.ApplicationJsonSequence, ContentTypes.TextPlain)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status202Accepted)]
        [EgressValidation]
        public async Task<ActionResult> CaptureParameters(
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
            string? tags = null)
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

        [HttpGet("stacks", Name = nameof(CaptureStacks))]
        [ProducesWithProblemDetails(ContentTypes.ApplicationJson, ContentTypes.TextPlain, ContentTypes.ApplicationSpeedscopeJson)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status202Accepted)]
        [EgressValidation]
        public Task<ActionResult> CaptureStacks(
            [FromQuery]
            int? pid = null,
            [FromQuery]
            Guid? uid = null,
            [FromQuery]
            string? name = null,
            [FromQuery]
            string? egressProvider = null,
            [FromQuery]
            string? tags = null)
        {
            if (!_callStacksOptions.Value.GetEnabled())
            {
                return Task.FromResult<ActionResult>(this.FeatureNotEnabled(Strings.FeatureName_CallStacks));
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

        private Task<ActionResult> StartTrace(
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

        private Task<ActionResult> StartLogs(
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
