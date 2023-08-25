// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using static Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.BoxingTokens;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing
{
    /// <summary>
    /// This decoder is made specifically for:
    /// - TypeReference'd value-types (e.g. enums from another assembly).
    ///
    /// The results of this decoder should not be used for any types not listed above.
    /// </summary>
    internal sealed class BoxingTokensSignatureProvider : ISignatureTypeProvider<uint, object?>
    {
        public static Lazy<BoxingTokensSignatureProvider> Instance = new(new BoxingTokensSignatureProvider());

        private readonly uint _unsupportedToken = SpecialCaseBoxingTypes.Unknown.BoxingToken();
        public uint GetArrayType(uint elementType, ArrayShape shape) => _unsupportedToken;
        public uint GetByReferenceType(uint elementType) => _unsupportedToken;
        public uint GetFunctionPointerType(MethodSignature<uint> signature) => _unsupportedToken;
        public uint GetGenericInstantiation(uint genericType, ImmutableArray<uint> typeArguments) => _unsupportedToken;
        public uint GetGenericMethodParameter(object? genericContext, int index) => _unsupportedToken;
        public uint GetGenericTypeParameter(object? genericContext, int index) => _unsupportedToken;
        public uint GetModifiedType(uint modifier, uint unmodifiedType, bool isRequired) => _unsupportedToken;
        public uint GetPinnedType(uint elementType) => _unsupportedToken;
        public uint GetPointerType(uint elementType) => _unsupportedToken;
        public uint GetPrimitiveType(PrimitiveTypeCode typeCode) => _unsupportedToken;
        public uint GetSZArrayType(uint elementType) => _unsupportedToken;
        public uint GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => _unsupportedToken;
        public uint GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => (uint)MetadataTokens.GetToken(handle);
        public uint GetTypeFromSpecification(MetadataReader reader, object? genericContext, TypeSpecificationHandle handle, byte rawTypeKind) => _unsupportedToken;
    }
}
