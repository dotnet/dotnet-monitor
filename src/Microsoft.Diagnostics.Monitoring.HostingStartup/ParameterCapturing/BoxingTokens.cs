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
        private const uint SpecialCaseBoxingTypeFlag = 0xff000000;
        private const uint UnsupportedParameterToken = SpecialCaseBoxingTypeFlag | (uint)SpecialCaseBoxingTypes.Unknown;
        private const uint SkipBoxingToken = SpecialCaseBoxingTypeFlag | (uint)SpecialCaseBoxingTypes.Object;

        private enum SpecialCaseBoxingTypes
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
            List<uint> boxingTokens = new List<uint>(methodParameterTypes.Count);

            // Handle implicit this
            if (method.CallingConvention.HasFlag(CallingConventions.HasThis))
            {
                Debug.Assert(!method.IsStatic);

                Type? thisType = method.DeclaringType;
                if (thisType != null)
                {
                    methodParameterTypes.Insert(0, thisType);
                }
                else
                {
                    boxingTokens.Add(UnsupportedParameterToken);
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
                else if (paramType.IsPrimitive)
                {
                    boxingTokens.Add(GetSpecialCaseBoxingToken(Type.GetTypeCode(paramType)));
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
                else if (paramType.IsGenericParameter)
                {
                    boxingTokens.Add(UnsupportedParameterToken);
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

        private static uint GetSpecialCaseBoxingToken(TypeCode typeCode)
        {
            return SpecialCaseBoxingTypeFlag | (uint)GetSpecialCaseBoxingType(typeCode);
        }

        private static SpecialCaseBoxingTypes GetSpecialCaseBoxingType(TypeCode typeCode)
        {
            return typeCode switch
            {
                TypeCode.Object => SpecialCaseBoxingTypes.Object,
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
