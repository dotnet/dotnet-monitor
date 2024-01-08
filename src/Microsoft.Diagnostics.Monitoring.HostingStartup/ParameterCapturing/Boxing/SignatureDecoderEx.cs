// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.FunctionProbes;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.Boxing
{
    internal static class SignatureDecoderEx
    {
        public static ParameterBoxingInstructions[] GetParameterBoxingInstructions(this MethodDefinition methodDef, MetadataReader mdReader)
        {
            BlobReader blobReader = mdReader.GetBlobReader(methodDef.Signature);
            SignatureDecoder<ParameterBoxingInstructions, object?> signatureDecoder = new(new BoxingTokensSignatureProvider(), mdReader, genericContext: null);

            return [.. DecodeMethodSignature(signatureDecoder, ref blobReader).ParameterTypes];
        }

        /// <summary>
        /// This is modified version of SignatureDecoder.DecodeMethodSignature that captures the memory regions of specific parameter signatures.
        ///
        /// Original source: https://github.com/dotnet/runtime/blob/5535e31a712343a63f5d7d796cd874e563e5ac14/src/libraries/System.Reflection.Metadata/src/System/Reflection/Metadata/Ecma335/SignatureDecoder.cs#L164
        /// </summary>
        private static MethodSignature<ParameterBoxingInstructions> DecodeMethodSignature(SignatureDecoder<ParameterBoxingInstructions, object?> decoder, ref BlobReader blobReader)
        {
            SignatureHeader header = blobReader.ReadSignatureHeader();
            // This is an expanded version of SignatureDecoder.CheckMethodOrPropertyHeader since that method is not public.
            if (header.Kind != SignatureKind.Method)
            {
                throw new Exception("oops");
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
                    // We need to check for the vararg sentinel value before decoding the type.
                    // However SignatureDecoder's public APIs does not expose the overloaded DecodeType method which accepts an already read type code.
                    // To handle this rewind the blob reader after checking for the vararg sentinel.
                    int curOffset = blobReader.Offset;
                    if (blobReader.ReadCompressedInteger() == (int)SignatureTypeCode.Sentinel)
                    {
                        break;
                    }
                    blobReader.Offset = curOffset;

                    parameterBuilder.Add(DecodeNextParameterBoxingInstructions(decoder, ref blobReader));
                }

                requiredParameterCount = parameterIndex;
                for (; parameterIndex < parameterCount; parameterIndex++)
                {
                    parameterBuilder.Add(DecodeNextParameterBoxingInstructions(decoder, ref blobReader));
                }
                parameterTypes = parameterBuilder.MoveToImmutable();
            }

            return new MethodSignature<ParameterBoxingInstructions>(header, returnType, requiredParameterCount, genericParameterCount, parameterTypes);
        }

        /// <summary>
        /// Parameter boxing instructions will sometimes require the memory region where the parameter signature lives.
        /// This method captures that information when needed.
        /// </summary>
        /// <returns></returns>
        private static unsafe ParameterBoxingInstructions DecodeNextParameterBoxingInstructions(SignatureDecoder<ParameterBoxingInstructions, object?> decoder, ref BlobReader blobReader)
        {
            byte* start = blobReader.CurrentPointer;
            ParameterBoxingInstructions instructions = decoder.DecodeType(ref blobReader);
            if (instructions.InstructionType != InstructionType.TypeSpec)
            {
                // Nothing extra to do
                return instructions;
            }

            // Parameters that are marked as TypeSpecs will need the signature buffer information, fill in that information.
            byte* end = blobReader.CurrentPointer;

            long signatureLength = end - start;
            if (signatureLength > uint.MaxValue)
            {
                // Signature is beyond a reasonable length, mark as unsupported.
                Debug.Fail("Parameter signature is too large");
                return SpecialCaseBoxingTypes.Unknown;
            }

            instructions.SignatureBufferPointer = start;
            instructions.SignatureBufferLength = (uint)signatureLength;

            return instructions;
        }
    }
}
