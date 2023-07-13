// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing
{
    internal static class BoxingTokens
    {
        private static readonly uint UnsupportedParameterToken = SpecialCaseBoxingTypes.Unknown.BoxingToken();
        private static readonly uint SkipBoxingToken = SpecialCaseBoxingTypes.Object.BoxingToken();

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
                    boxingTokens.Add(GetSpecialCaseBoxingTokenForPrimitive(Type.GetTypeCode(paramType)));
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
                        boxingTokens.Add(UnsupportedParameterToken);
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
            }

            return boxingTokens.ToArray();
        }

        private static uint GetSpecialCaseBoxingTokenForPrimitive(TypeCode typeCode)
        {
            return GetSpecialCaseBoxingTypeForPrimitive(typeCode).BoxingToken();
        }

        private static SpecialCaseBoxingTypes GetSpecialCaseBoxingTypeForPrimitive(TypeCode typeCode)
        {
            return typeCode switch
            {
                TypeCode.Object => SpecialCaseBoxingTypes.Unknown, // IntPtr, UIntPtr
                TypeCode.Boolean => SpecialCaseBoxingTypes.Boolean,
                TypeCode.Char => SpecialCaseBoxingTypes.Char,
                TypeCode.SByte => SpecialCaseBoxingTypes.SByte,
                TypeCode.Byte => SpecialCaseBoxingTypes.Byte,
                TypeCode.Int16 => SpecialCaseBoxingTypes.Int16,
                TypeCode.UInt16 => SpecialCaseBoxingTypes.UInt16,
                TypeCode.Int32 => SpecialCaseBoxingTypes.Int32,
                TypeCode.UInt32 => SpecialCaseBoxingTypes.UInt32,
                TypeCode.Int64 => SpecialCaseBoxingTypes.Int64,
                TypeCode.UInt64 => SpecialCaseBoxingTypes.UInt64,
                TypeCode.Single => SpecialCaseBoxingTypes.Single,
                TypeCode.Double => SpecialCaseBoxingTypes.Double,
                _ => SpecialCaseBoxingTypes.Unknown,
            };
        }
    }
}
