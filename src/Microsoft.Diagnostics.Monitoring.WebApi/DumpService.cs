// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal class DumpService : IDumpService
    {
        private readonly ConcurrentDictionary<IEndpointInfo, OperationsTracker> _operations = new();
        private readonly IOptionsMonitor<DiagnosticPortOptions> _portOptions;
        private readonly IOptionsMonitor<StorageOptions> _storageOptions;

        public DumpService(
            IOptionsMonitor<StorageOptions> storageOptions,
            IOptionsMonitor<DiagnosticPortOptions> portOptions)
        {
            _portOptions = portOptions ?? throw new ArgumentNullException(nameof(portOptions));
            _storageOptions = storageOptions ?? throw new ArgumentNullException(nameof(StorageOptions));
        }

        public async Task<Stream> DumpAsync(IEndpointInfo endpointInfo, Models.DumpType mode, CancellationToken token)
        {
            if (endpointInfo == null)
            {
                throw new ArgumentNullException(nameof(endpointInfo));
            }

            string dumpFilePath = Path.Combine(_storageOptions.CurrentValue.DumpTempFolder, FormattableString.Invariant($"{Guid.NewGuid()}_{endpointInfo.ProcessId}"));
            DumpType dumpType = MapDumpType(mode);

            IDisposable operationRegistration = null;
            // Only track operation status for endpoints from a listening server because:
            // 1) Each process only ever has a single instance of an IEndpointInfo
            // 2) Only the listening server will query the dump service for the operation status of an endpoint.
            if (IsListenMode)
            {
                // This is a quick fix to prevent the polling algorithm in the ServerEndpointInfoSource
                // from removing IEndpointInfo instances when they don't respond in a timely manner to
                // a dump operation causing the runtime to temporarily be unresponsive. Long term, this
                // concept should be folded into RequestLimitTracker with registered endpoint information
                // and allowing to query the status of an endpoint for a given artifact.
                operationRegistration = GetOperationsTracker(endpointInfo).Register();
            }

            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // Get the process
                    Process process = Process.GetProcessById(endpointInfo.ProcessId);
                    await Dumper.CollectDumpAsync(process, dumpFilePath, dumpType);
                }
                else
                {
                    var client = new DiagnosticsClient(endpointInfo.Endpoint);
                    await client.WriteDumpAsync(dumpType, dumpFilePath, logDumpGeneration: false, token);
                }
            }
            finally
            {
                operationRegistration?.Dispose();
            }

            return new AutoDeleteFileStream(dumpFilePath);
        }

        public bool IsExecutingOperation(IEndpointInfo endpointInfo)
        {
            if (IsListenMode)
            {
                return GetOperationsTracker(endpointInfo).IsExecutingOperation;
            }
            return false;
        }

        public void EndpointRemoved(IEndpointInfo endpointInfo)
        {
            _operations.TryRemove(endpointInfo, out _);
        }

        private OperationsTracker GetOperationsTracker(IEndpointInfo endpointInfo)
        {
            return _operations.GetOrAdd(endpointInfo, _ => new OperationsTracker());
        }

        private bool IsListenMode => _portOptions.CurrentValue.GetConnectionMode() == DiagnosticPortConnectionMode.Listen;

        /// <summary>
        /// We want to make sure we destroy files we finish streaming.
        /// We want to make sure that we stream out files since we compress on the fly; the size cannot be known upfront.
        /// CONSIDER The above implies knowledge of how the file is used by the rest api.
        /// </summary>
        private sealed class AutoDeleteFileStream : FileStream
        {
            public AutoDeleteFileStream(string path) : base(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete,
                bufferSize: 4096, FileOptions.DeleteOnClose)
            {
            }

            public override bool CanSeek => false;
        }

        private static DumpType MapDumpType(Models.DumpType dumpType)
        {
            switch (dumpType)
            {
                case Models.DumpType.Full:
                    return DumpType.Full;
                case Models.DumpType.WithHeap:
                    return DumpType.WithHeap;
                case Models.DumpType.Triage:
                    return DumpType.Triage;
                case Models.DumpType.Mini:
                    return DumpType.Normal;
                default:
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            Strings.ErrorMessage_UnexpectedType,
                            nameof(DumpType),
                            dumpType),
                        nameof(dumpType));
            }
        }

        private sealed class OperationsTracker : IDisposable
        {
            private long _count = 0;

            public IDisposable Register()
            {
                Interlocked.Increment(ref _count);
                return this;
            }

            public bool IsExecutingOperation => 0 != Interlocked.Read(ref _count);

            public void Dispose() => Interlocked.Decrement(ref _count);
        }
    }
}
