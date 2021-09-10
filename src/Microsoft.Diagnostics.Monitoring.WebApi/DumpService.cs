// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Extensions.Options;
using System;
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
        private readonly IOptionsMonitor<StorageOptions> _storageOptions;

        public DumpService(IOptionsMonitor<StorageOptions> storageOptions)
        {
            _storageOptions = storageOptions;
        }

        public async Task<Stream> DumpAsync(IEndpointInfo endpointInfo, Models.DumpType mode, CancellationToken token)
        {
            string dumpFilePath = Path.Combine(_storageOptions.CurrentValue.DumpTempFolder, FormattableString.Invariant($"{Guid.NewGuid()}_{endpointInfo.ProcessId}"));
            DumpType dumpType = MapDumpType(mode);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Get the process
                Process process = Process.GetProcessById(endpointInfo.ProcessId);
                    await Dumper.CollectDumpAsync(process, dumpFilePath, dumpType);
            }
            else
            {
                await Task.Run(async () =>
                {
                    var client = new DiagnosticsClient(endpointInfo.Endpoint);
                    await client.WriteDumpAsync(dumpType, dumpFilePath, logDumpGeneration: false, token);
                });
            }

            return new AutoDeleteFileStream(dumpFilePath);
        }

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
    }
}
