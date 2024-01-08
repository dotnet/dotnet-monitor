// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.FunctionProbes;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing
{
    internal static class SignatureDecoderEx
    {
        public static unsafe MethodSignature<ParameterBoxingInstructions> DecodeMethodSignature(SignatureDecoder<ParameterBoxingInstructions, object?> decoder, ref BlobReader blobReader)
        {
            SignatureHeader header = blobReader.ReadSignatureHeader();
            // TODO: Add comment this came from runtime
            if (header.Kind != SignatureKind.Method)
            {
                // TODO: Replace with better exception
                throw new Exception();
            }

            int genericParameterCount = 0;
            if (header.IsGeneric)
            {
                genericParameterCount = blobReader.ReadCompressedInteger();
            }

            int parameterCount = blobReader.ReadCompressedInteger();
            ParameterBoxingInstructions returnType = decoder.DecodeType(ref blobReader);
            ImmutableArray<ParameterBoxingInstructions> parameterTypes;
            int requiredParameterCount;

            if (parameterCount == 0)
            {
                requiredParameterCount = 0;
                parameterTypes = ImmutableArray<ParameterBoxingInstructions>.Empty;
            }
            else
            {
                var parameterBuilder = ImmutableArray.CreateBuilder<ParameterBoxingInstructions>(parameterCount);
                int parameterIndex;

                for (parameterIndex = 0; parameterIndex < parameterCount; parameterIndex++)
                {
                    // TODO: Comment change
                    int curOffset = blobReader.Offset;
                    if (blobReader.ReadCompressedInteger() == (int)SignatureTypeCode.Sentinel)
                    {
                        break;
                    }
                    blobReader.Offset = curOffset;

                    ParameterBoxingInstructions instructions = GetNextParameterBoxingInstructions(decoder, ref blobReader);
                    parameterBuilder.Add(instructions);
                }

                requiredParameterCount = parameterIndex;
                for (; parameterIndex < parameterCount; parameterIndex++)
                {
                    // TODO: Comment change
                    ParameterBoxingInstructions instructions = GetNextParameterBoxingInstructions(decoder, ref blobReader);
                    parameterBuilder.Add(instructions);
                }
                parameterTypes = parameterBuilder.MoveToImmutable();
            }

            return new MethodSignature<ParameterBoxingInstructions>(header, returnType, requiredParameterCount, genericParameterCount, parameterTypes);
        }

        private static unsafe ParameterBoxingInstructions GetNextParameterBoxingInstructions(SignatureDecoder<ParameterBoxingInstructions, object?> decoder, ref BlobReader blobReader)
        {
            byte* start = blobReader.CurrentPointer;
            ParameterBoxingInstructions instructions = decoder.DecodeType(ref blobReader);
            byte* end = blobReader.CurrentPointer;

            long signatureLength = end - start;
            if (signatureLength > uint.MaxValue)
            {
                Debug.Fail("TODO: Fix message");
                // TODO: Consider if we want to handle the null pointer in managed (not mark it as typespec)
            }
            else
            {
                instructions.SignatureBuffer = new IntPtr(start);
                instructions.SignatureLength = (uint)signatureLength;
            }

            return instructions;
        }
    }
}
