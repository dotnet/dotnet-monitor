// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.FunctionProbes
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
        MetadataToken
    }

    internal struct ParameterBoxingInstructions
    {
        public InstructionType InstructionType;

        public uint Token;

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
