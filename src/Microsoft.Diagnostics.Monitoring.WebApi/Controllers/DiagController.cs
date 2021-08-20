// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using FastSerialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.WebApi.Validation;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
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
    public partial class DiagController : ControllerBase
    {
        public const string ArtifactType_Dump = "dump";
        public const string ArtifactType_GCDump = "gcdump";
        public const string ArtifactType_Logs = "logs";
        public const string ArtifactType_Trace = "trace";
        public const string ArtifactType_LiveMetrics = "livemetrics";

        private const Models.TraceProfile DefaultTraceProfiles = Models.TraceProfile.Cpu | Models.TraceProfile.Http | Models.TraceProfile.Metrics;
        private static readonly MediaTypeHeaderValue NdJsonHeader = new MediaTypeHeaderValue(ContentTypes.ApplicationNdJson);
        private static readonly MediaTypeHeaderValue JsonSequenceHeader = new MediaTypeHeaderValue(ContentTypes.ApplicationJsonSequence);
        private static readonly MediaTypeHeaderValue EventStreamHeader = new MediaTypeHeaderValue(ContentTypes.TextEventStream);

        private readonly ILogger<DiagController> _logger;
        private readonly IDiagnosticServices _diagnosticServices;
        private readonly IOptions<DiagnosticPortOptions> _diagnosticPortOptions;
        private readonly EgressOperationStore _operationsStore;

        public DiagController(ILogger<DiagController> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _diagnosticServices = serviceProvider.GetRequiredService<IDiagnosticServices>();
            _diagnosticPortOptions = serviceProvider.GetService<IOptions<DiagnosticPortOptions>>();
            _operationsStore = serviceProvider.GetRequiredService<EgressOperationStore>();
        }

        /// <summary>
        /// Get the list of accessible processes.
        /// </summary>
        [HttpGet("processes", Name = nameof(GetProcesses))]
        [ProducesWithProblemDetails(ContentTypes.ApplicationJson)]
        [ProducesResponseType(typeof(IEnumerable<Models.ProcessIdentifier>), StatusCodes.Status200OK)]
        public Task<ActionResult<IEnumerable<Models.ProcessIdentifier>>> GetProcesses()
        {
            return this.InvokeService(async () =>
            {
                IProcessInfo defaultProcessInfo = null;
                try
                {
                    defaultProcessInfo = await _diagnosticServices.GetProcessAsync(null, HttpContext.RequestAborted);
                }
                catch (Exception)
                {
                    // Unable to locate a default process; no action required
                }

                IList<Models.ProcessIdentifier> processesIdentifiers = new List<Models.ProcessIdentifier>();
                foreach (IProcessInfo p in await _diagnosticServices.GetProcessesAsync(processFilter: null, HttpContext.RequestAborted))
                {
                    processesIdentifiers.Add(new Models.ProcessIdentifier()
                    {
                        Pid = p.EndpointInfo.ProcessId,
                        Uid = p.EndpointInfo.RuntimeInstanceCookie,
                        Name = p.ProcessName,
                        IsDefault = (defaultProcessInfo != null &&
                            p.EndpointInfo.ProcessId == defaultProcessInfo.EndpointInfo.ProcessId &&
                            p.EndpointInfo.RuntimeInstanceCookie == defaultProcessInfo.EndpointInfo.RuntimeInstanceCookie)
                    });
                }
                _logger.WrittenToHttpStream();
                return new ActionResult<IEnumerable<Models.ProcessIdentifier>>(processesIdentifiers);
            }, _logger);
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
            string name = null)
        {
            ProcessKey? processKey = GetProcessKey(pid, uid, name);

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

                _logger.WrittenToHttpStream();

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
            string name = null)
        {
            ProcessKey? processKey = GetProcessKey(pid, uid, name);

            return InvokeForProcess<Dictionary<string, string>>(processInfo =>
            {
                var client = new DiagnosticsClient(processInfo.EndpointInfo.Endpoint);

                try
                {
                    Dictionary<string, string> environment = client.GetProcessEnvironment();

                    _logger.WrittenToHttpStream();

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
        /// <returns></returns>
        [HttpGet("dump", Name = nameof(CaptureDump))]
        [ProducesWithProblemDetails(ContentTypes.ApplicationOctetStream)]
        // FileResult is the closest representation of the output so that the OpenAPI document correctly
        // describes the result as a binary file.
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status202Accepted)]
        [RequestLimit(LimitKey = ArtifactType_Dump)]
        [EgressValidation]
        public Task<ActionResult> CaptureDump(
            [FromQuery]
            int? pid = null,
            [FromQuery]
            Guid? uid = null,
            [FromQuery]
            string name = null,
            [FromQuery]
            Models.DumpType type = Models.DumpType.WithHeap,
            [FromQuery]
            string egressProvider = null)
        {
            ProcessKey? processKey = GetProcessKey(pid, uid, name);

            return InvokeForProcess(async processInfo =>
            {
                string dumpFileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                    FormattableString.Invariant($"dump_{GetFileNameTimeStampUtcNow()}.dmp") :
                    FormattableString.Invariant($"core_{GetFileNameTimeStampUtcNow()}");

                if (string.IsNullOrEmpty(egressProvider))
                {
                    Stream dumpStream = await _diagnosticServices.GetDump(processInfo, type, HttpContext.RequestAborted);

                    _logger.WrittenToHttpStream();
                    //Compression is done automatically by the response
                    //Chunking is done because the result has no content-length
                    return File(dumpStream, ContentTypes.ApplicationOctetStream, dumpFileName);
                }
                else
                {
                    KeyValueLogScope scope = new KeyValueLogScope();
                    scope.AddArtifactType(ArtifactType_Dump);
                    scope.AddEndpointInfo(processInfo.EndpointInfo);

                    return await SendToEgress(new EgressOperation(
                        token => _diagnosticServices.GetDump(processInfo, type, token),
                        egressProvider,
                        dumpFileName,
                        processInfo.EndpointInfo,
                        ContentTypes.ApplicationOctetStream,
                        scope), limitKey: ArtifactType_Dump);
                }
            }, processKey, ArtifactType_Dump);
        }

        /// <summary>
        /// Capture a GC dump of a process.
        /// </summary>
        /// <param name="pid">Process ID used to identify the target process.</param>
        /// <param name="uid">The Runtime instance cookie used to identify the target process.</param>
        /// <param name="name">Process name used to identify the target process.</param>
        /// <param name="egressProvider">The egress provider to which the GC dump is saved.</param>
        /// <returns></returns>
        [HttpGet("gcdump", Name = nameof(CaptureGcDump))]
        [ProducesWithProblemDetails(ContentTypes.ApplicationOctetStream)]
        // FileResult is the closest representation of the output so that the OpenAPI document correctly
        // describes the result as a binary file.
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status202Accepted)]
        [RequestLimit(LimitKey = ArtifactType_GCDump)]
        [EgressValidation]
        public Task<ActionResult> CaptureGcDump(
            [FromQuery]
            int? pid = null,
            [FromQuery]
            Guid? uid = null,
            [FromQuery]
            string name = null,
            [FromQuery]
            string egressProvider = null)
        {
            ProcessKey? processKey = GetProcessKey(pid, uid, name);

            return InvokeForProcess(processInfo =>
            {
                string fileName = FormattableString.Invariant($"{GetFileNameTimeStampUtcNow()}_{processInfo.EndpointInfo.ProcessId}.gcdump");

                Func<CancellationToken, Task<IFastSerializable>> action = async (token) => {
                    var graph = new Graphs.MemoryGraph(50_000);

                    EventGCPipelineSettings settings = new EventGCPipelineSettings
                    {
                        Duration = Timeout.InfiniteTimeSpan,
                    };

                    var client = new DiagnosticsClient(processInfo.EndpointInfo.Endpoint);

                    await using var pipeline = new EventGCDumpPipeline(client, settings, graph);
                    await pipeline.RunAsync(token);

                    return new GCHeapDump(graph)
                    {
                        CreationTool = "dotnet-monitor"
                    };
                };

                return Result(
                    ArtifactType_GCDump,
                    egressProvider,
                    ConvertFastSerializeAction(action),
                    fileName,
                    ContentTypes.ApplicationOctetStream,
                    processInfo.EndpointInfo);
            }, processKey, ArtifactType_GCDump);
        }

        /// <summary>
        /// Capture a trace of a process.
        /// </summary>
        /// <param name="pid">Process ID used to identify the target process.</param>
        /// <param name="uid">The Runtime instance cookie used to identify the target process.</param>
        /// <param name="name">Process name used to identify the target process.</param>
        /// <param name="profile">The profiles enabled for the trace session.</param>
        /// <param name="durationSeconds">The duration of the trace session (in seconds).</param>
        /// <param name="metricsIntervalSeconds">The reporting interval (in seconds) for event counters.</param>
        /// <param name="egressProvider">The egress provider to which the trace is saved.</param>
        [HttpGet("trace", Name = nameof(CaptureTrace))]
        [ProducesWithProblemDetails(ContentTypes.ApplicationOctetStream)]
        // FileResult is the closest representation of the output so that the OpenAPI document correctly
        // describes the result as a binary file.
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status202Accepted)]
        [RequestLimit(LimitKey = ArtifactType_Trace)]
        [EgressValidation]
        public Task<ActionResult> CaptureTrace(
            [FromQuery]
            int? pid = null,
            [FromQuery]
            Guid? uid = null,
            [FromQuery]
            string name = null,
            [FromQuery]
            Models.TraceProfile profile = DefaultTraceProfiles,
            [FromQuery][Range(-1, int.MaxValue)]
            int durationSeconds = 30,
            [FromQuery][Range(1, int.MaxValue)]
            int metricsIntervalSeconds = 1,
            [FromQuery]
            string egressProvider = null)
        {
            ProcessKey? processKey = GetProcessKey(pid, uid, name);

            return InvokeForProcess(processInfo =>
            {
                TimeSpan duration = ConvertSecondsToTimeSpan(durationSeconds);

                var configurations = new List<MonitoringSourceConfiguration>();
                if (profile.HasFlag(Models.TraceProfile.Cpu))
                {
                    configurations.Add(new CpuProfileConfiguration());
                }
                if (profile.HasFlag(Models.TraceProfile.Http))
                {
                    configurations.Add(new HttpRequestSourceConfiguration());
                }
                if (profile.HasFlag(Models.TraceProfile.Logs))
                {
                    configurations.Add(new LoggingSourceConfiguration(
                        LogLevel.Trace,
                        LogMessageType.FormattedMessage | LogMessageType.JsonMessage,
                        filterSpecs: null,
                        useAppFilters: true));
                }
                if (profile.HasFlag(Models.TraceProfile.Metrics))
                {
                    configurations.Add(new MetricSourceConfiguration(metricsIntervalSeconds, Enumerable.Empty<string>()));
                }

                var aggregateConfiguration = new AggregateSourceConfiguration(configurations.ToArray());

                return StartTrace(processInfo, aggregateConfiguration, duration, egressProvider);
            }, processKey, ArtifactType_Trace);
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
        [HttpPost("trace", Name = nameof(CaptureTraceCustom))]
        [ProducesWithProblemDetails(ContentTypes.ApplicationOctetStream)]
        // FileResult is the closest representation of the output so that the OpenAPI document correctly
        // describes the result as a binary file.
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status202Accepted)]
        [RequestLimit(LimitKey = ArtifactType_Trace)]
        [EgressValidation]
        public Task<ActionResult> CaptureTraceCustom(
            [FromBody][Required]
            Models.EventPipeConfiguration configuration,
            [FromQuery]
            int? pid = null,
            [FromQuery]
            Guid? uid = null,
            [FromQuery]
            string name = null,
            [FromQuery][Range(-1, int.MaxValue)]
            int durationSeconds = 30,
            [FromQuery]
            string egressProvider = null)
        {
            ProcessKey? processKey = GetProcessKey(pid, uid, name);

            return InvokeForProcess(processInfo =>
            {
                TimeSpan duration = ConvertSecondsToTimeSpan(durationSeconds);

                var providers = new List<EventPipeProvider>();

                foreach (Models.EventPipeProvider providerModel in configuration.Providers)
                {
                    if (!IntegerOrHexStringAttribute.TryParse(providerModel.Keywords, out long keywords, out string parseError))
                    {
                        throw new InvalidOperationException(parseError);
                    }

                    providers.Add(new EventPipeProvider(
                        providerModel.Name,
                        providerModel.EventLevel,
                        keywords,
                        providerModel.Arguments
                        ));
                }

                var traceConfiguration = new EventPipeProviderSourceConfiguration(
                    providers: providers.ToArray(),
                    requestRundown: configuration.RequestRundown,
                    bufferSizeInMB: configuration.BufferSizeInMB);

                return StartTrace(processInfo, traceConfiguration, duration, egressProvider);
            }, processKey, ArtifactType_Trace);
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
        [HttpGet("logs", Name = nameof(CaptureLogs))]
        [ProducesWithProblemDetails(ContentTypes.ApplicationNdJson, ContentTypes.ApplicationJsonSequence, ContentTypes.TextEventStream)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status202Accepted)]
        [RequestLimit(LimitKey = ArtifactType_Logs)]
        [EgressValidation]
        public Task<ActionResult> CaptureLogs(
            [FromQuery]
            int? pid = null,
            [FromQuery]
            Guid? uid = null,
            [FromQuery]
            string name = null,
            [FromQuery][Range(-1, int.MaxValue)]
            int durationSeconds = 30,
            [FromQuery]
            LogLevel? level = null,
            [FromQuery]
            string egressProvider = null)
        {
            ProcessKey? processKey = GetProcessKey(pid, uid, name);

            return InvokeForProcess(processInfo =>
            {
                TimeSpan duration = ConvertSecondsToTimeSpan(durationSeconds);

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

                return StartLogs(processInfo, settings, egressProvider);
            }, processKey, ArtifactType_Logs);
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
        [HttpPost("logs", Name = nameof(CaptureLogsCustom))]
        [ProducesWithProblemDetails(ContentTypes.ApplicationNdJson, ContentTypes.ApplicationJsonSequence, ContentTypes.TextEventStream)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(void), StatusCodes.Status202Accepted)]
        [RequestLimit(LimitKey = ArtifactType_Logs)]
        [EgressValidation]
        public Task<ActionResult> CaptureLogsCustom(
            [FromBody]
            Models.LogsConfiguration configuration,
            [FromQuery]
            int? pid = null,
            [FromQuery]
            Guid? uid = null,
            [FromQuery]
            string name = null,
            [FromQuery][Range(-1, int.MaxValue)]
            int durationSeconds = 30,
            [FromQuery]
            string egressProvider = null)
        {
            ProcessKey? processKey = GetProcessKey(pid, uid, name);

            return InvokeForProcess(processInfo =>
            {
                TimeSpan duration = ConvertSecondsToTimeSpan(durationSeconds);

                var settings = new EventLogsPipelineSettings()
                {
                    Duration = duration,
                    FilterSpecs = configuration.FilterSpecs,
                    LogLevel = configuration.LogLevel,
                    UseAppFilters = configuration.UseAppFilters
                };

                return StartLogs(processInfo, settings, egressProvider);
            }, processKey, ArtifactType_Logs);
        }

        /// <summary>
        /// Gets versioning and listening mode information about Dotnet-Monitor 
        /// </summary>
        [HttpGet("info", Name = nameof(GetInfo))]
        [ProducesWithProblemDetails(ContentTypes.ApplicationJson)]
        [ProducesResponseType(typeof(Models.DotnetMonitorInfo), StatusCodes.Status200OK)]
        public ActionResult<Models.DotnetMonitorInfo> GetInfo()
        {
            return this.InvokeService(() =>
            {
                string version = GetDotnetMonitorVersion();
                string runtimeVersion = Environment.Version.ToString();
                DiagnosticPortConnectionMode diagnosticPortMode = GetDiagnosticPortMode();
                string diagnosticPortName = GetDiagnosticPortName();

                Models.DotnetMonitorInfo dotnetMonitorInfo = new Models.DotnetMonitorInfo()
                {
                    Version = version,
                    RuntimeVersion = runtimeVersion,
                    DiagnosticPortMode = diagnosticPortMode,
                    DiagnosticPortName = diagnosticPortName
                };

                _logger.WrittenToHttpStream();
                return new ActionResult<Models.DotnetMonitorInfo>(dotnetMonitorInfo);
            }, _logger);
        }

        private static string GetDotnetMonitorVersion()
        {
            var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();

            var assemblyVersionAttribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();

            if (assemblyVersionAttribute is null)
            {
                return assembly.GetName().Version.ToString();
            }
            else
            {
                return assemblyVersionAttribute.InformationalVersion;
            }
        }

        public DiagnosticPortConnectionMode GetDiagnosticPortMode()
        {
            return _diagnosticPortOptions.Value.ConnectionMode.GetValueOrDefault(DiagnosticPortOptionsDefaults.ConnectionMode);
        }

        public string GetDiagnosticPortName()
        {
            return _diagnosticPortOptions.Value.EndpointName;
        }

        private Task<ActionResult> StartTrace(
            IProcessInfo processInfo,
            MonitoringSourceConfiguration configuration,
            TimeSpan duration,
            string egressProvider)
        {
            string fileName = FormattableString.Invariant($"{GetFileNameTimeStampUtcNow()}_{processInfo.EndpointInfo.ProcessId}.nettrace");

            Func<Stream, CancellationToken, Task> action = async (outputStream, token) =>
            {
                Func<Stream, CancellationToken, Task> streamAvailable = async (Stream eventStream, CancellationToken token) =>
                {
                    //Buffer size matches FileStreamResult
                    //CONSIDER Should we allow client to change the buffer size?
                    await eventStream.CopyToAsync(outputStream, 0x10000, token);
                };

                var client = new DiagnosticsClient(processInfo.EndpointInfo.Endpoint);

                await using EventTracePipeline pipeProcessor = new EventTracePipeline(client, new EventTracePipelineSettings
                {
                    Configuration = configuration,
                    Duration = duration,
                }, streamAvailable);

                await pipeProcessor.RunAsync(token);
            };

            return Result(
                ArtifactType_Trace,
                egressProvider,
                action,
                fileName,
                ContentTypes.ApplicationOctetStream,
                processInfo.EndpointInfo);
        }

        private Task<ActionResult> StartLogs(
            IProcessInfo processInfo,
            EventLogsPipelineSettings settings,
            string egressProvider)
        {
            LogFormat format = ComputeLogFormat(Request.GetTypedHeaders().Accept);
            if (format == LogFormat.None)
            {
                return Task.FromResult<ActionResult>(this.NotAcceptable());
            }

            string fileName = FormattableString.Invariant($"{GetFileNameTimeStampUtcNow()}_{processInfo.EndpointInfo.ProcessId}.txt");
            string contentType = ContentTypes.TextEventStream;

            if (format == LogFormat.EventStream)
            {
                contentType = ContentTypes.TextEventStream;
            }
            else if (format == LogFormat.NDJson)
            {
                contentType = ContentTypes.ApplicationNdJson;
            }
            else if (format == LogFormat.JsonSequence)
            {
                contentType = ContentTypes.ApplicationJsonSequence;
            }

            Func<Stream, CancellationToken, Task> action = async (outputStream, token) =>
            {
                using var loggerFactory = new LoggerFactory();

                loggerFactory.AddProvider(new StreamingLoggerProvider(outputStream, format, logLevel: null));

                var client = new DiagnosticsClient(processInfo.EndpointInfo.Endpoint);

                await using EventLogsPipeline pipeline = new EventLogsPipeline(client, settings, loggerFactory);
                await pipeline.RunAsync(token);
            };

            return Result(
                ArtifactType_Logs,
                egressProvider,
                action,
                fileName,
                contentType,
                processInfo.EndpointInfo,
                format != LogFormat.EventStream);
        }

        private static TimeSpan ConvertSecondsToTimeSpan(int durationSeconds)
        {
            return durationSeconds < 0 ?
                Timeout.InfiniteTimeSpan :
                TimeSpan.FromSeconds(durationSeconds);
        }

        private static ProcessKey? GetProcessKey(int? pid, Guid? uid, string name)
        {
            return (pid == null && uid == null && name == null) ? null : new ProcessKey(pid, uid, name);
        }

        private static string GetFileNameTimeStampUtcNow()
        {
            return DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        }

        private static LogFormat ComputeLogFormat(IList<MediaTypeHeaderValue> acceptedHeaders)
        {
            if (acceptedHeaders == null)
            {
                return LogFormat.None;
            }

            if (acceptedHeaders.Contains(EventStreamHeader))
            {
                return LogFormat.EventStream;
            }
            if (acceptedHeaders.Contains(NdJsonHeader))
            {
                return LogFormat.NDJson;
            }
            if (acceptedHeaders.Contains(JsonSequenceHeader))
            {
                return LogFormat.JsonSequence;
            }
            if (acceptedHeaders.Any(h => EventStreamHeader.IsSubsetOf(h)))
            {
                return LogFormat.EventStream;
            }
            if (acceptedHeaders.Any(h => NdJsonHeader.IsSubsetOf(h)))
            {
                return LogFormat.NDJson;
            }
            if (acceptedHeaders.Any(h => JsonSequenceHeader.IsSubsetOf(h)))
            {
                return LogFormat.JsonSequence;
            }
            return LogFormat.None;
        }

        private Task<ActionResult> Result(
            string artifactType,
            string providerName,
            Func<Stream, CancellationToken, Task> action,
            string fileName,
            string contentType,
            IEndpointInfo endpointInfo,
            bool asAttachment = true)
        {
            KeyValueLogScope scope = new KeyValueLogScope();
            scope.AddArtifactType(artifactType);
            scope.AddEndpointInfo(endpointInfo);

            if (string.IsNullOrEmpty(providerName))
            {
                return Task.FromResult<ActionResult>(new OutputStreamResult(
                    action,
                    contentType,
                    asAttachment ? fileName : null,
                    scope));
            }
            else
            {
                return SendToEgress(new EgressOperation(
                    action,
                    providerName,
                    fileName,
                    endpointInfo,
                    contentType,
                    scope),
                    limitKey: artifactType);
            }
        }

        private async Task<ActionResult> SendToEgress(EgressOperation egressStreamResult, string limitKey)
        {
            // Will throw TooManyRequestsException if there are too many concurrent operations.
            Guid operationId = await _operationsStore.AddOperation(egressStreamResult, limitKey);
            string newUrl = this.Url.Action(
                action: nameof(OperationsController.GetOperationStatus),
                controller: OperationsController.ControllerName, new { operationId = operationId },
                protocol: this.HttpContext.Request.Scheme, this.HttpContext.Request.Host.ToString());

            return Accepted(newUrl);
        }

        private static Func<Stream, CancellationToken, Task> ConvertFastSerializeAction(Func<CancellationToken, Task<IFastSerializable>> action)
        {
            return async (stream, token) =>
            {
                IFastSerializable fastSerializable = await action(token);

                // FastSerialization requests the length of the stream before serializing to the stream.
                // If the stream is a response stream, requesting the length or setting the position is
                // not supported. Create an intermediate buffer if testing the stream fails.
                // This can use a huge amount of memory if the IFastSerializable is very large.
                // CONSIDER: Update FastSerialization to not get the length or attempt to reset the position.
                bool useIntermediateStream = false;
                try
                {
                    _ = stream.Length;
                }
                catch (NotSupportedException)
                {
                    useIntermediateStream = true;
                }

                if (useIntermediateStream)
                {
                    using var intermediateStream = new MemoryStream();

                    var serializer = new Serializer(intermediateStream, fastSerializable, leaveOpen: true);
                    serializer.Close();

                    intermediateStream.Position = 0;

                    await intermediateStream.CopyToAsync(stream, 0x10000, token);
                }
                else
                {
                    var serializer = new Serializer(stream, fastSerializable, leaveOpen: true);
                    serializer.Close();
                }
            };
        }

        private Task<ActionResult> InvokeForProcess(Func<IProcessInfo, ActionResult> func, ProcessKey? processKey, string artifactType = null)
        {
            Func<IProcessInfo, Task<ActionResult>> asyncFunc =
                processInfo => Task.FromResult(func(processInfo));

            return InvokeForProcess(asyncFunc, processKey, artifactType);
        }

        private async Task<ActionResult> InvokeForProcess(Func<IProcessInfo, Task<ActionResult>> func, ProcessKey? processKey, string artifactType)
        {
            ActionResult<object> result = await InvokeForProcess<object>(async processInfo => await func(processInfo), processKey, artifactType);

            return result.Result;
        }

        private Task<ActionResult<T>> InvokeForProcess<T>(Func<IProcessInfo, ActionResult<T>> func, ProcessKey? processKey, string artifactType = null)
        {
            return InvokeForProcess(processInfo => Task.FromResult(func(processInfo)), processKey, artifactType);
        }

        private async Task<ActionResult<T>> InvokeForProcess<T>(Func<IProcessInfo, Task<ActionResult<T>>> func, ProcessKey? processKey, string artifactType = null)
        {
            IDisposable artifactTypeRegistration = null;
            if (!string.IsNullOrEmpty(artifactType))
            {
                KeyValueLogScope artifactTypeScope = new KeyValueLogScope();
                artifactTypeScope.AddArtifactType(artifactType);
                artifactTypeRegistration = _logger.BeginScope(artifactTypeScope);
            }

            try
            {
                return await this.InvokeService(async () =>
                {
                    IProcessInfo processInfo = await _diagnosticServices.GetProcessAsync(processKey, HttpContext.RequestAborted);

                    KeyValueLogScope processInfoScope = new KeyValueLogScope();
                    processInfoScope.AddEndpointInfo(processInfo.EndpointInfo);
                    using var _ = _logger.BeginScope(processInfoScope);

                    _logger.ResolvedTargetProcess();

                    return await func(processInfo);
                }, _logger);
            }
            finally
            {
                artifactTypeRegistration?.Dispose();
            }
        }
    }
}