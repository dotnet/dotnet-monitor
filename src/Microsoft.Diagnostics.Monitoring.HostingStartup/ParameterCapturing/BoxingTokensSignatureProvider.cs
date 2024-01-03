// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    internal sealed class BoxingTokensSignatureProvider : ISignatureTypeProvider<uint?, object?>
    {
        //
        // Supported
        //
        public uint? GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => (uint)MetadataTokens.GetToken(handle);

        //
        // Unsupported
        //
        public uint? GetArrayType(uint? elementType, ArrayShape shape) => null;
        public uint? GetByReferenceType(uint? elementType) => null;
        public uint? GetFunctionPointerType(MethodSignature<uint?> signature) => null;
        public uint? GetGenericInstantiation(uint? genericType, ImmutableArray<uint?> typeArguments) => null;
        public uint? GetGenericMethodParameter(object? genericContext, int index) => null;
        public uint? GetGenericTypeParameter(object? genericContext, int index) => null;
        public uint? GetModifiedType(uint? modifier, uint? unmodifiedType, bool isRequired) => null;
        public uint? GetPinnedType(uint? elementType) => null;
        public uint? GetPointerType(uint? elementType) => null;
        public uint? GetPrimitiveType(PrimitiveTypeCode typeCode) => null;
        public uint? GetSZArrayType(uint? elementType) => null;
        public uint? GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => null;
        public uint? GetTypeFromSpecification(MetadataReader reader, object? genericContext, TypeSpecificationHandle handle, byte rawTypeKind) => null;
    }
}
