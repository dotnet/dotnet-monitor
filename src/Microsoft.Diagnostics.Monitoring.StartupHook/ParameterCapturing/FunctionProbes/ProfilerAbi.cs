// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// !!IMPORTANT!!
// All types in this file are also used by the mutating profiler during PINVOKEs and so **need** to be kept in sync
// with the profiler's version (found in src\Profilers\MutatingMonitorProfiler\ProbeInstrumentation\ProbeInjector.h).
//

using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing.FunctionProbes
{
    internal enum SpecialCaseBoxingTypes : uint
    {
        Unknown = 0,
        Object,
        Boolean,
        Char,
        SByte,
        Byte,
        Int16,
        UInt16,
        Int32,
        UInt32,
        Int64,
        UInt64,
        IntPtr,
        UIntPtr,
        Single,
        Double,
    };

    internal enum InstructionType : ushort
    {
        Unknown = 0,
        SpecialCaseToken,
        MetadataToken,
        TypeSpec
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct ParameterBoxingInstructions
    {
        public InstructionType InstructionType;

        //
        // NOTE: The profiler represents this Token field as a union for easier type handling.
        // However it takes extra steps (static asserts) to ensure that the union is the same
        // size as a uint so it won't impact the size of the struct or field offsets.
        //
        public uint Token;

        public byte* SignatureBufferPointer;
        public uint SignatureBufferLength;

        public static implicit operator ParameterBoxingInstructions(uint mdToken)
        {
            return new ParameterBoxingInstructions()
            {
                InstructionType = InstructionType.MetadataToken,
                Token = mdToken
            };
        }

        public static implicit operator ParameterBoxingInstructions(SpecialCaseBoxingTypes token)
        {
            return new ParameterBoxingInstructions()
            {
                InstructionType = InstructionType.SpecialCaseToken,
                Token = (uint)token
            };
        }
    }
}
