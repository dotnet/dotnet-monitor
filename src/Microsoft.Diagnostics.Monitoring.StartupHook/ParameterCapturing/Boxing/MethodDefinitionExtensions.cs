// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing.FunctionProbes;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing.Boxing
{
    internal static class MethodDefinitionExtensions
    {
        public static ParameterBoxingInstructions[] GetParameterBoxingInstructions(this MethodDefinition methodDef, MetadataReader mdReader)
        {
            BlobReader blobReader = mdReader.GetBlobReader(methodDef.Signature);
            SignatureDecoder<ParameterBoxingInstructions, object?> signatureDecoder = new(new BoxingTokensSignatureProvider(), mdReader, genericContext: null);

            return [.. DecodeMethodSignature(signatureDecoder, ref blobReader).ParameterTypes];
        }

        /// <summary>
        /// This is modified version of SignatureDecoder.DecodeMethodSignature that captures the memory regions of specific parameter signatures.
        /// Any changes are denoted by comments above the relevant lines explaining why they were necessary.
        ///
        /// Original source: https://github.com/dotnet/runtime/blob/5535e31a712343a63f5d7d796cd874e563e5ac14/src/libraries/System.Reflection.Metadata/src/System/Reflection/Metadata/Ecma335/SignatureDecoder.cs#L164
        /// </summary>
        private static MethodSignature<ParameterBoxingInstructions> DecodeMethodSignature(SignatureDecoder<ParameterBoxingInstructions, object?> decoder, ref BlobReader blobReader)
        {
            SignatureHeader header = blobReader.ReadSignatureHeader();
            // This is an expanded version of SignatureDecoder.CheckMethodOrPropertyHeader since that method is not public.
            if (header.Kind != SignatureKind.Method)
            {
                throw new BadImageFormatException(ParameterCapturingStrings.ErrorMessage_SignatureIsNotForAMethod);
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

                    // Updated to extract the memory region of the parameter's signature when needed. 
                    parameterBuilder.Add(DecodeNextParameterBoxingInstructions(decoder, ref blobReader));
                }

                requiredParameterCount = parameterIndex;
                for (; parameterIndex < parameterCount; parameterIndex++)
                {
                    // Updated to extract the memory region of the parameter's signature when needed. 
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

            //
            // We return the pointer to native memory used by the blob reader (returned by AssemblyExtensions.TryGetRawMetadata).
            // The metadata memory will remain valid as long as the assembly its for is alive.
            //
            // We store the MethodInfo for every method being actively instrumented, which holds a reference to the assembly it belongs to (method.Module.Assembly)
            // this ensures that the pointers we're using here will remain valid as long as we're instrumenting the assembly it belongs to.
            //
            // With regards to assemblies that belong to a collectible (unloadable) AssemblyLoadContext, the ALC won't allow a full unload
            // to occur while there are still references to the assembly in question so we do not need to worry about unloads here.
            // ref: https://github.com/dotnet/runtime/blob/v8.0.1/docs/design/features/unloadability.md#assemblyloadcontext-unloading-process
            //

            instructions.SignatureBufferPointer = start;
            instructions.SignatureBufferLength = (uint)signatureLength;

            return instructions;
        }
    }
}
