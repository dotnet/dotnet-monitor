// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.FunctionProbes;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing
{
    /// <summary>
    /// This decoder is made specifically for parameters of the following types:
    /// - TypeReference'd value-types (e.g. enums from another assembly).
    ///
    /// The results of this decoder should not be used for any types not listed above.
    /// </summary>
    internal sealed class BoxingTokensSignatureProvider : ISignatureTypeProvider<ParameterBoxingInstructions, object?>
    {
        //
        // Supported
        //
        public ParameterBoxingInstructions GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => (uint)MetadataTokens.GetToken(handle);

        //
        // Unsupported
        //
        public ParameterBoxingInstructions GetArrayType(ParameterBoxingInstructions elementType, ArrayShape shape) => SpecialCaseBoxingTypes.Unknown;
        public ParameterBoxingInstructions GetByReferenceType(ParameterBoxingInstructions elementType) => SpecialCaseBoxingTypes.Unknown;
        public ParameterBoxingInstructions GetFunctionPointerType(MethodSignature<ParameterBoxingInstructions> signature) => SpecialCaseBoxingTypes.Unknown;
        public ParameterBoxingInstructions GetGenericInstantiation(ParameterBoxingInstructions genericType, ImmutableArray<ParameterBoxingInstructions> typeArguments) => SpecialCaseBoxingTypes.Unknown;
        public ParameterBoxingInstructions GetGenericMethodParameter(object? genericContext, int index) => SpecialCaseBoxingTypes.Unknown;
        public ParameterBoxingInstructions GetGenericTypeParameter(object? genericContext, int index) => SpecialCaseBoxingTypes.Unknown;
        public ParameterBoxingInstructions GetModifiedType(ParameterBoxingInstructions modifier, ParameterBoxingInstructions unmodifiedType, bool isRequired) => SpecialCaseBoxingTypes.Unknown;
        public ParameterBoxingInstructions GetPinnedType(ParameterBoxingInstructions elementType) => SpecialCaseBoxingTypes.Unknown;
        public ParameterBoxingInstructions GetPointerType(ParameterBoxingInstructions elementType) => SpecialCaseBoxingTypes.Unknown;
        public ParameterBoxingInstructions GetPrimitiveType(PrimitiveTypeCode typeCode) => SpecialCaseBoxingTypes.Unknown;
        public ParameterBoxingInstructions GetSZArrayType(ParameterBoxingInstructions elementType) => SpecialCaseBoxingTypes.Unknown;
        public ParameterBoxingInstructions GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => SpecialCaseBoxingTypes.Unknown;
        public ParameterBoxingInstructions GetTypeFromSpecification(MetadataReader reader, object? genericContext, TypeSpecificationHandle handle, byte rawTypeKind) => SpecialCaseBoxingTypes.Unknown;
    }
}
