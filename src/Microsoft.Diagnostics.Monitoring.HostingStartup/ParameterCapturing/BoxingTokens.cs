// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing
{
    internal static class BoxingTokens
    {
        public static readonly uint UnsupportedParameterToken = SpecialCaseBoxingTypes.Unknown.BoxingToken();
        public static readonly uint SkipBoxingToken = SpecialCaseBoxingTypes.Object.BoxingToken();

        public enum SpecialCaseBoxingTypes
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

        public static uint BoxingToken(this SpecialCaseBoxingTypes specialCase)
        {
            const uint SpecialCaseBoxingTypeFlag = 0x7f000000;
            return SpecialCaseBoxingTypeFlag | (uint)specialCase;
        }

        public static bool IsParameterSupported(uint token)
        {
            return token != UnsupportedParameterToken;
        }

        public static bool[] AreParametersSupported(uint[] tokens)
        {
            bool[] supported = new bool[tokens.Length];
            for (int i = 0; i < supported.Length; i++)
            {
                supported[i] = IsParameterSupported(tokens[i]);
            }

            return supported;
        }

        public static uint[] GetBoxingTokens(MethodInfo method)
        {
            List<Type> methodParameterTypes = method.GetParameters().Select(p => p.ParameterType).ToList();
            List<uint> boxingTokens = new List<uint>(methodParameterTypes.Count + (method.HasImplicitThis() ? 1 : 0));

            //
            // A signature decoder used to determine boxing tokens for parameter types that cannot be determined from standard
            // reflection alone. The boxing tokens generated from this decoder should only be used to fill in these gaps
            // as it is not a comprhensive decoder and will produce invalid tokens for any types not explicitly mentioned
            // in BoxingTokensSignatureProvider's summary.
            // 
            Lazy<uint[]?> ancillaryBoxingTokens = new(() => GetAncillaryBoxingTokensFromMethodSignature(method));

            int formalParameterPosition = 0;
            // Handle implicit this
            if (method.HasImplicitThis())
            {
                Debug.Assert(!method.IsStatic);

                Type? thisType = method.DeclaringType;
                if (thisType == null ||
                    thisType.IsValueType)
                {
                    //
                    // Implicit this pointers for value types can **sometimes** be passed as an address to the value.
                    // For now don't support this scenario.
                    //
                    // To enable it in the future add a new special case token and when rewriting IL
                    // emit a ldobj instruction for it.
                    //
                    boxingTokens.Add(UnsupportedParameterToken);
                }
                else
                {
                    methodParameterTypes.Insert(0, thisType);
                    // Implicit this isn't a formal parameter.
                    formalParameterPosition = -1;
                }
            }

            foreach (Type paramType in methodParameterTypes)
            {
                if (paramType.IsByRef ||
                    paramType.IsByRefLike ||
                    paramType.IsPointer)
                {
                    boxingTokens.Add(UnsupportedParameterToken);
                }
                else if (paramType.IsGenericParameter)
                {
                    boxingTokens.Add(UnsupportedParameterToken);
                }
                else if (paramType.IsPrimitive)
                {
                    boxingTokens.Add(GetSpecialCaseBoxingTokenForPrimitive(paramType));
                }
                else if (paramType.IsValueType)
                {
                    // Ref structs have already been filtered out by the above IsByRefLike check.
                    if (paramType.IsGenericType)
                    {
                        // Typespec
                        boxingTokens.Add(UnsupportedParameterToken);
                    }
                    else if (paramType.Assembly != method.Module.Assembly)
                    {
                        // Typeref
                        if (formalParameterPosition >= 0)
                        {
                            // value-type type refs are supported by the signature decoder
                            boxingTokens.Add(ancillaryBoxingTokens.Value?[formalParameterPosition] ?? UnsupportedParameterToken);
                        }
                        else
                        {
                            boxingTokens.Add(UnsupportedParameterToken);
                        }
                    }
                    else
                    {
                        // Typedef
                        boxingTokens.Add((uint)paramType.MetadataToken);
                    }
                }
                else if (paramType.IsArray ||
                    paramType.IsClass ||
                    paramType.IsInterface)
                {
                    boxingTokens.Add(SkipBoxingToken);
                }
                else
                {
                    boxingTokens.Add(UnsupportedParameterToken);
                }
                formalParameterPosition++;
            }

            return boxingTokens.ToArray();
        }


        private static unsafe uint[]? GetAncillaryBoxingTokensFromMethodSignature(MethodInfo method)
        {
            try
            {
                Assembly assembly = method.Module.Assembly;
                if (!assembly.TryGetRawMetadata(out byte* pMdBlob, out int mdLength))
                {
                    return null;
                }

                MetadataReader mdReader = new(pMdBlob, mdLength);

                MethodDefinitionHandle methodDefHandle = (MethodDefinitionHandle)MetadataTokens.Handle(method.MetadataToken);
                MethodDefinition methodDef = mdReader.GetMethodDefinition(methodDefHandle);

                MethodSignature<uint> methodSignature = methodDef.DecodeSignature(new BoxingTokensSignatureProvider(), genericContext: null);

                return methodSignature.ParameterTypes.ToArray();
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static uint GetSpecialCaseBoxingTokenForPrimitive(Type primitiveType)
        {
            SpecialCaseBoxingTypes boxingType = SpecialCaseBoxingTypes.Unknown;
            if (primitiveType == typeof(sbyte))
            {
                boxingType = SpecialCaseBoxingTypes.SByte;
            }
            else if (primitiveType == typeof(byte))
            {
                boxingType = SpecialCaseBoxingTypes.Byte;
            }
            else if (primitiveType == typeof(short))
            {
                boxingType = SpecialCaseBoxingTypes.Int16;
            }
            else if (primitiveType == typeof(ushort))
            {
                boxingType = SpecialCaseBoxingTypes.UInt16;
            }
            else if (primitiveType == typeof(int))
            {
                boxingType = SpecialCaseBoxingTypes.Int32;
            }
            else if (primitiveType == typeof(uint))
            {
                boxingType = SpecialCaseBoxingTypes.UInt32;
            }
            else if (primitiveType == typeof(long))
            {
                boxingType = SpecialCaseBoxingTypes.Int64;
            }
            else if (primitiveType == typeof(ulong))
            {
                boxingType = SpecialCaseBoxingTypes.UInt64;
            }
            else if (primitiveType == typeof(bool))
            {
                boxingType = SpecialCaseBoxingTypes.Boolean;
            }
            else if (primitiveType == typeof(char))
            {
                boxingType = SpecialCaseBoxingTypes.Char;
            }
            else if (primitiveType == typeof(float))
            {
                boxingType = SpecialCaseBoxingTypes.Single;
            }
            else if (primitiveType == typeof(double))
            {
                boxingType = SpecialCaseBoxingTypes.Double;
            }
            else if (primitiveType == typeof(IntPtr))
            {
                boxingType = SpecialCaseBoxingTypes.IntPtr;
            }
            else if (primitiveType == typeof(UIntPtr))
            {
                boxingType = SpecialCaseBoxingTypes.UIntPtr;
            }

            return boxingType.BoxingToken();
        }
    }
}
