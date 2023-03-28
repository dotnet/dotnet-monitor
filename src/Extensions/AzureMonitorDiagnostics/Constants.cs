// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Monitoring.AzureMonitorDiagnostics;

internal static class Constants
{
    public static class Provider
    {
        public const string Name = "AzureMonitorDiagnostics";
    }

    /// <summary>
    /// Blob metadata property names.
    /// </summary>
    public static class Metadata
    {
        /// <summary>
        /// The name of the machine on which the artifact was collected.
        /// </summary>
        /// <remarks>
        /// Maps to the Role Instance.
        /// </remarks>
        public const string MachineName = "spMachineName";

        /// <summary>
        /// The operating system where the agent is running.
        /// </summary>
        /// <example>Windows</example>
        /// <example>Linux</example>
        public const string OSPlatform = "spOSPlatform";

        /// <summary>
        /// The processor architecture of the running app.
        /// </summary>
        public const string ProcessorArch = nameof(ProcessorArch);

        /// <summary>
        /// The role name of the associated application or service.
        /// This distinguishes services that are sharing a single instrumentation key.
        /// </summary>
        public const string RoleName = nameof(RoleName);

        /// <summary>
        /// For profiles, the trace file format.
        /// </summary>
        /// <remarks>
        /// See <see cref="TraceFormat"/> for values.
        /// </remarks>
        public const string TraceFileFormat = "spTraceFileFormat";

        /// <summary>
        /// The start time of a profiler trace in UTC.
        /// The value should be formatted in ISO8601 format.
        /// </summary>
        public const string TraceStartTime = "spTraceStartTime";

        /// <summary>
        /// The trigger that created the uploaded artifact.
        /// This should be a simple one-word string.
        /// </summary>
        /// <example>OnDemand</example>
        /// <example>Sample</example>
        /// <example>HighCPU</example>
        /// <example>Exception</example>
        public const string TriggerType = nameof(TriggerType);
    }

    /// <summary>
    /// Possible values for <see cref="Metadata.TraceFileFormat"/>.
    /// </summary>
    public static class TraceFormat
    {
        /// <summary>
        /// The .NET trace file format.
        /// </summary>
        public const string NetTrace = "nettrace";
    }
}
