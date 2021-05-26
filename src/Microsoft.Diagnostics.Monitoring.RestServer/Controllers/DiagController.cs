// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using FastSerialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.RestServer.Validation;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.RestServer.Controllers
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
    public class DiagController : ControllerBase
    {
        private const string ArtifactType_Dump = "dump";
        private const string ArtifactType_GCDump = "gcdump";
        private const string ArtifactType_Logs = "logs";
        private const string ArtifactType_Trace = "trace";

        private const Models.TraceProfile DefaultTraceProfiles = Models.TraceProfile.Cpu | Models.TraceProfile.Http | Models.TraceProfile.Metrics;
        private static readonly MediaTypeHeaderValue NdJsonHeader = new MediaTypeHeaderValue(ContentTypes.ApplicationNdJson);
        private static readonly MediaTypeHeaderValue EventStreamHeader = new MediaTypeHeaderValue(ContentTypes.TextEventStream);

        private readonly ILogger<DiagController> _logger;
        private readonly IDiagnosticServices _diagnosticServices;

        public DiagController(ILogger<DiagController> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _diagnosticServices = serviceProvider.GetRequiredService<IDiagnosticServices>();
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
                IList<Models.ProcessIdentifier> processesIdentifiers = new List<Models.ProcessIdentifier>();
                foreach (IProcessInfo p in await _diagnosticServices.GetProcessesAsync(processFilter: null, HttpContext.RequestAborted))
                {
                    processesIdentifiers.Add(new Models.ProcessIdentifier()
                    {
                        Pid = p.EndpointInfo.ProcessId,
                        Uid = p.EndpointInfo.RuntimeInstanceCookie,
                        Name = p.ProcessName
                    });
                }
                _logger.WrittenToHttpStream();
                return new ActionResult<IEnumerable<Models.ProcessIdentifier>>(processesIdentifiers);
            }, _logger);
        }

        /// <summary>
        /// Get information about the specified process.
        /// </summary>
        /// <param name="processKey">Value used to identify the target process, either the process ID, the runtime instance cookie, or process name.</param>
        [HttpGet("processes/{processKey}", Name = nameof(GetProcessInfo))]
        [ProducesWithProblemDetails(ContentTypes.ApplicationJson)]
        [ProducesResponseType(typeof(Models.ProcessInfo), StatusCodes.Status200OK)]
        public Task<ActionResult<Models.ProcessInfo>> GetProcessInfo(
            ProcessKey processKey)
        {
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
        /// <param name="processKey">Value used to identify the target process, either the process ID, the runtime instance cookie, or process name.</param>
        [HttpGet("processes/{processKey}/env", Name = nameof(GetProcessEnvironment))]
        [ProducesWithProblemDetails(ContentTypes.ApplicationJson)]
        [ProducesResponseType(typeof(Dictionary<string, string>), StatusCodes.Status200OK)]
        public Task<ActionResult<Dictionary<string, string>>> GetProcessEnvironment(
            ProcessKey processKey)
        {
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
                    throw new InvalidOperationException("Unable to get process environment.");
                }
            },
            processKey);
        }

        /// <summary>
        /// Capture a dump of a process.
        /// </summary>
        /// <param name="processKey">Value used to identify the target process, either the process ID, the runtime instance cookie, or process name.</param>
        /// <param name="type">The type of dump to capture.</param>
        /// <param name="egressProvider">The egress provider to which the dump is saved.</param>
        /// <returns></returns>
        [HttpGet("dump/{processKey?}", Name = nameof(CaptureDump))]
        [ProducesWithProblemDetails(ContentTypes.ApplicationOctetStream)]
        // FileResult is the closest representation of the output so that the OpenAPI document correctly
        // describes the result as a binary file.
        [ProducesResponseType(typeof(void), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        [RequestLimit(MaxConcurrency = 1)]
        public Task<ActionResult> CaptureDump(
            ProcessKey? processKey,
            [FromQuery]
            Models.DumpType type = Models.DumpType.WithHeap,
            [FromQuery]
            string egressProvider = null)
        {
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

                    return new EgressStreamResult(
                        token => _diagnosticServices.GetDump(processInfo, type, token),
                        egressProvider,
                        dumpFileName,
                        processInfo.EndpointInfo,
                        ContentTypes.ApplicationOctetStream,
                        scope);
                }
            }, processKey, ArtifactType_Dump);
        }

        /// <summary>
        /// Capture a GC dump of a process.
        /// </summary>
        /// <param name="processKey">Value used to identify the target process, either the process ID, the runtime instance cookie, or process name.</param>
        /// <param name="egressProvider">The egress provider to which the GC dump is saved.</param>
        /// <returns></returns>
        [HttpGet("gcdump/{processKey?}", Name = nameof(CaptureGcDump))]
        [ProducesWithProblemDetails(ContentTypes.ApplicationOctetStream)]
        // FileResult is the closest representation of the output so that the OpenAPI document correctly
        // describes the result as a binary file.
        [ProducesResponseType(typeof(void), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        [RequestLimit(MaxConcurrency = 1)]
        public Task<ActionResult> CaptureGcDump(
            ProcessKey? processKey,
            [FromQuery]
            string egressProvider = null)
        {
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
        /// <param name="processKey">Value used to identify the target process, either the process ID, the runtime instance cookie, or process name.</param>
        /// <param name="profile">The profiles enabled for the trace session.</param>
        /// <param name="durationSeconds">The duration of the trace session (in seconds).</param>
        /// <param name="metricsIntervalSeconds">The reporting interval (in seconds) for event counters.</param>
        /// <param name="egressProvider">The egress provider to which the trace is saved.</param>
        [HttpGet("trace/{processKey?}", Name = nameof(CaptureTrace))]
        [ProducesWithProblemDetails(ContentTypes.ApplicationOctetStream)]
        // FileResult is the closest representation of the output so that the OpenAPI document correctly
        // describes the result as a binary file.
        [ProducesResponseType(typeof(void), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        [RequestLimit(MaxConcurrency = 3)]
        public Task<ActionResult> CaptureTrace(
            ProcessKey? processKey,
            [FromQuery]
            Models.TraceProfile profile = DefaultTraceProfiles,
            [FromQuery][Range(-1, int.MaxValue)]
            int durationSeconds = 30,
            [FromQuery][Range(1, int.MaxValue)]
            int metricsIntervalSeconds = 1,
            [FromQuery]
            string egressProvider = null)
        {
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
        /// <param name="processKey">Value used to identify the target process, either the process ID, the runtime instance cookie, or process name.</param>
        /// <param name="configuration">The trace configuration describing which events to capture.</param>
        /// <param name="durationSeconds">The duration of the trace session (in seconds).</param>
        /// <param name="egressProvider">The egress provider to which the trace is saved.</param>
        [HttpPost("trace/{processKey?}", Name = nameof(CaptureTraceCustom))]
        [ProducesWithProblemDetails(ContentTypes.ApplicationOctetStream)]
        // FileResult is the closest representation of the output so that the OpenAPI document correctly
        // describes the result as a binary file.
        [ProducesResponseType(typeof(void), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        [RequestLimit(MaxConcurrency = 3)]
        public Task<ActionResult> CaptureTraceCustom(
            ProcessKey? processKey,
            [FromBody][Required]
            Models.EventPipeConfiguration configuration,
            [FromQuery][Range(-1, int.MaxValue)]
            int durationSeconds = 30,
            [FromQuery]
            string egressProvider = null)
        {
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
        /// <param name="processKey">Value used to identify the target process, either the process ID, the runtime instance cookie, or process name.</param>
        /// <param name="durationSeconds">The duration of the logs session (in seconds).</param>
        /// <param name="level">The level of the logs to capture.</param>
        /// <param name="egressProvider">The egress provider to which the logs are saved.</param>
        [HttpGet("logs/{processKey?}", Name = nameof(CaptureLogs))]
        [ProducesWithProblemDetails(ContentTypes.ApplicationNdJson, ContentTypes.TextEventStream)]
        [ProducesResponseType(typeof(void), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [RequestLimit(MaxConcurrency = 3)]
        public Task<ActionResult> CaptureLogs(
            ProcessKey? processKey,
            [FromQuery][Range(-1, int.MaxValue)]
            int durationSeconds = 30,
            [FromQuery]
            LogLevel? level = null,
            [FromQuery]
            string egressProvider = null)
        {
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
        /// <param name="processKey">Value used to identify the target process, either the process ID, the runtime instance cookie, or process name.</param>
        /// <param name="configuration">The logs configuration describing which logs to capture.</param>
        /// <param name="durationSeconds">The duration of the logs session (in seconds).</param>
        /// <param name="egressProvider">The egress provider to which the logs are saved.</param>
        [HttpPost("logs/{processKey?}", Name = nameof(CaptureLogsCustom))]
        [ProducesWithProblemDetails(ContentTypes.ApplicationNdJson, ContentTypes.TextEventStream)]
        [ProducesResponseType(typeof(void), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [RequestLimit(MaxConcurrency = 3)]
        public Task<ActionResult> CaptureLogsCustom(
            ProcessKey? processKey,
            [FromBody]
            Models.LogsConfiguration configuration,
            [FromQuery][Range(-1, int.MaxValue)]
            int durationSeconds = 30,
            [FromQuery]
            string egressProvider = null)
        {
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

        private ActionResult StartTrace(
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

        private ActionResult StartLogs(
            IProcessInfo processInfo,
            EventLogsPipelineSettings settings,
            string egressProvider)
        {
            LogFormat format = ComputeLogFormat(Request.GetTypedHeaders().Accept);
            if (format == LogFormat.None)
            {
                return this.NotAcceptable();
            }

            string fileName = FormattableString.Invariant($"{GetFileNameTimeStampUtcNow()}_{processInfo.EndpointInfo.ProcessId}.txt");
            string contentType = format == LogFormat.EventStream ? ContentTypes.TextEventStream : ContentTypes.ApplicationNdJson;

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
                return LogFormat.Json;
            }
            if (acceptedHeaders.Any(h => EventStreamHeader.IsSubsetOf(h)))
            {
                return LogFormat.EventStream;
            }
            if (acceptedHeaders.Any(h => NdJsonHeader.IsSubsetOf(h)))
            {
                return LogFormat.Json;
            }
            return LogFormat.None;
        }

        private ActionResult Result(
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
                return new OutputStreamResult(
                    action,
                    contentType,
                    asAttachment ? fileName : null,
                    scope);
            }
            else
            {
                return new EgressStreamResult(
                    action,
                    providerName,
                    fileName,
                    endpointInfo,
                    contentType,
                    scope);
            }
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
