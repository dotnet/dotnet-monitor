// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.FileFormats;
using Microsoft.FileFormats.ELF;
using Microsoft.FileFormats.MachO;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    public static class DumpTestUtilities
    {
        public const string EnableElfDumpOnMacOS = "COMPlus_DbgEnableElfDumpOnMacOS";

        public static async Task ValidateDump(bool expectElfDump, Stream dumpStream)
        {
            Assert.NotNull(dumpStream);

            byte[] headerBuffer = new byte[64];

            // Read enough to deserialize the header.
            int read;
            int total = 0;
            using CancellationTokenSource cancellation = new(CommonTestTimeouts.DumpTimeout);
            while (total < headerBuffer.Length && 0 != (read = await dumpStream.ReadAsync(headerBuffer, total, headerBuffer.Length - total, cancellation.Token)))
            {
                total += read;
            }
            Assert.Equal(headerBuffer.Length, total);

            // Read header and validate
            using MemoryStream headerStream = new(headerBuffer);

            StreamAddressSpace dumpAddressSpace = new(headerStream);
            Reader dumpReader = new(dumpAddressSpace);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                MinidumpHeader header = dumpReader.Read<MinidumpHeader>(0);
                // Validate Signature
                Assert.True(header.IsSignatureValid.Check());
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                ELFHeaderIdent ident = dumpReader.Read<ELFHeaderIdent>(0);
                Assert.True(ident.IsIdentMagicValid.Check());
                Assert.True(ident.IsClassValid.Check());
                Assert.True(ident.IsDataValid.Check());

                LayoutManager layoutManager = new();
                layoutManager.AddELFTypes(
                    isBigEndian: ident.Data == ELFData.BigEndian,
                    is64Bit: ident.Class == ELFClass.Class64);
                Reader headerReader = new(dumpAddressSpace, layoutManager);

                ELFHeader header = headerReader.Read<ELFHeader>(0);
                // Validate Signature
                Assert.True(header.IsIdentMagicValid.Check());
                // Validate ELF file is a core dump
                Assert.Equal(ELFHeaderType.Core, header.Type);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                if (expectElfDump)
                {
                    ELFHeader header = dumpReader.Read<ELFHeader>(0);
                    // Validate Signature
                    Assert.True(header.IsIdentMagicValid.Check());
                    // Validate ELF file is a core dump
                    Assert.Equal(ELFHeaderType.Core, header.Type);
                }
                else
                {
                    MachHeader header = dumpReader.Read<MachHeader>(0);
                    // Validate Signature
                    Assert.True(header.IsMagicValid.Check());
                    // Validate MachO file is a core dump
                    Assert.True(header.IsFileTypeValid.Check());
                    Assert.Equal(MachHeaderFileType.Core, header.FileType);
                }
            }
            else
            {
                throw new NotImplementedException("Dump header check not implemented for this OS platform.");
            }
        }

        public class MinidumpHeader : TStruct
        {
            public uint Signature = 0;
            public uint Version = 0;
            public uint NumberOfStreams = 0;
            public uint StreamDirectoryRva = 0;
            public uint CheckSum = 0;
            public uint TimeDateStamp = 0;
            public ulong Flags = 0;

            // 50,4D,44,4D = PMDM
            public ValidationRule IsSignatureValid => new ValidationRule("Invalid Signature", () => Signature == 0x504D444DU);
        }
    }
}
