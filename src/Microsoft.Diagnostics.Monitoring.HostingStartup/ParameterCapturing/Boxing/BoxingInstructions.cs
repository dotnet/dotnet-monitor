// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.FunctionProbes;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.Boxing
{
    internal static class BoxingInstructions
    {
        public static bool IsParameterSupported(ParameterBoxingInstructions instructions)
        {
            return !(instructions.InstructionType == InstructionType.SpecialCaseToken && instructions.Token == (uint)SpecialCaseBoxingTypes.Unknown);
        }

        public static bool[] AreParametersSupported(ParameterBoxingInstructions[] tokens)
        {
            bool[] supported = new bool[tokens.Length];
            for (int i = 0; i < supported.Length; i++)
            {
                supported[i] = IsParameterSupported(tokens[i]);
            }

            return supported;
        }

        public static ParameterBoxingInstructions GetBoxingInstructionFromReflection(MethodInfo method, Type paramType, out bool canUseSignatureDecoder)
        {
            canUseSignatureDecoder = false;

            if (paramType.IsByRef || paramType.IsByRefLike || paramType.IsPointer)
            {
                return SpecialCaseBoxingTypes.Unknown;
            }

            if (paramType.IsGenericParameter)
            {
                canUseSignatureDecoder = true;
                return SpecialCaseBoxingTypes.Unknown;
            }

            if (paramType.IsPrimitive)
            {
                return GetSpecialCaseBoxingTokenForPrimitive(paramType);
            }

            if (paramType.IsValueType)
            {
                // Ref structs have already been filtered out by the above IsByRefLike check.
                if (paramType.IsGenericType ||
                    paramType.Assembly != method.Module.Assembly)
                {
                    // Typeref or Typespec
                    canUseSignatureDecoder = true;
                    return SpecialCaseBoxingTypes.Unknown;
                }
                else
                {
                    // Typedef
                    return (uint)paramType.MetadataToken;
                }
            }

            if (paramType.IsArray || paramType.IsClass || paramType.IsInterface)
            {
                return SpecialCaseBoxingTypes.Object;
            }

            return SpecialCaseBoxingTypes.Unknown;
        }
        public static ParameterBoxingInstructions[] GetBoxingInstructions(MethodInfo method)
        {
            ParameterInfo[] formalParameters = method.GetParameters();
            int numberOfParameters = formalParameters.Length + (method.HasImplicitThis() ? 1 : 0);
            if (numberOfParameters == 0)
            {
                return Array.Empty<ParameterBoxingInstructions>();
            }

            ParameterBoxingInstructions[] instructions = new ParameterBoxingInstructions[numberOfParameters];
            int index = 0;

            // Handle implicit this
            if (method.HasImplicitThis())
            {
                Debug.Assert(!method.IsStatic);

                ParameterBoxingInstructions thisBoxingInstructions;

                Type? thisType = method.DeclaringType;
                if (thisType == null || thisType.IsValueType)
                {
                    //
                    // Implicit this pointers for value types can **sometimes** be passed as an address to the value.
                    // For now don't support this scenario.
                    //
                    // To enable it in the future add a new special case token and when rewriting IL
                    // emit a ldobj instruction for it.
                    //
                    thisBoxingInstructions = SpecialCaseBoxingTypes.Unknown;
                }
                else
                {
                    thisBoxingInstructions = GetBoxingInstructionFromReflection(method, thisType, out _);
                }

                instructions[index++] = thisBoxingInstructions;
            }


            //
            // A signature decoder will used to determine boxing tokens for formal parameter types that cannot be determined from standard
            // reflection alone. The boxing tokens generated from this decoder should only be used to fill in these gaps
            // as it is not a comprehensive decoder and will produce an unsupported boxing instruction for any types not explicitly mentioned
            // in BoxingTokensSignatureProvider's summary.
            // 
            Lazy<ParameterBoxingInstructions[]?> signatureDecodingInstructions = new(() => GetFormalBoxingInstructionsFromMethodSignature(method));


            foreach (ParameterInfo paramInfo in formalParameters)
            {
                ParameterBoxingInstructions paramBoxingInstructions = GetBoxingInstructionFromReflection(method, paramInfo.ParameterType, out bool canUseSignatureDecoder);
                if (canUseSignatureDecoder &&
                    !IsParameterSupported(paramBoxingInstructions) &&
                    signatureDecodingInstructions.Value != null)
                {
                    paramBoxingInstructions = signatureDecodingInstructions.Value[paramInfo.Position];
                }

                instructions[index++] = paramBoxingInstructions;
            }

            Debug.Assert(index == instructions.Length);

            return instructions;
        }

        private static unsafe ParameterBoxingInstructions[]? GetFormalBoxingInstructionsFromMethodSignature(MethodInfo method)
        {
            try
            {
                if (!method.Module.Assembly.TryGetRawMetadata(out byte* pMdBlob, out int mdLength))
                {
                    return null;
                }

                MetadataReader mdReader = new(pMdBlob, mdLength);

                MethodDefinitionHandle methodDefHandle = (MethodDefinitionHandle)MetadataTokens.Handle(method.MetadataToken);
                MethodDefinition methodDef = mdReader.GetMethodDefinition(methodDefHandle);

                return methodDef.GetParameterBoxingInstructions(mdReader);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static SpecialCaseBoxingTypes GetSpecialCaseBoxingTokenForPrimitive(Type primitiveType)
        {
            if (primitiveType == typeof(sbyte))
            {
                return SpecialCaseBoxingTypes.SByte;
            }
            else if (primitiveType == typeof(byte))
            {
                return SpecialCaseBoxingTypes.Byte;
            }
            else if (primitiveType == typeof(short))
            {
                return SpecialCaseBoxingTypes.Int16;
            }
            else if (primitiveType == typeof(ushort))
            {
                return SpecialCaseBoxingTypes.UInt16;
            }
            else if (primitiveType == typeof(int))
            {
                return SpecialCaseBoxingTypes.Int32;
            }
            else if (primitiveType == typeof(uint))
            {
                return SpecialCaseBoxingTypes.UInt32;
            }
            else if (primitiveType == typeof(long))
            {
                return SpecialCaseBoxingTypes.Int64;
            }
            else if (primitiveType == typeof(ulong))
            {
                return SpecialCaseBoxingTypes.UInt64;
            }
            else if (primitiveType == typeof(bool))
            {
                return SpecialCaseBoxingTypes.Boolean;
            }
            else if (primitiveType == typeof(char))
            {
                return SpecialCaseBoxingTypes.Char;
            }
            else if (primitiveType == typeof(float))
            {
                return SpecialCaseBoxingTypes.Single;
            }
            else if (primitiveType == typeof(double))
            {
                return SpecialCaseBoxingTypes.Double;
            }
            else if (primitiveType == typeof(IntPtr))
            {
                return SpecialCaseBoxingTypes.IntPtr;
            }
            else if (primitiveType == typeof(UIntPtr))
            {
                return SpecialCaseBoxingTypes.UIntPtr;
            }

            return SpecialCaseBoxingTypes.Unknown;
        }
    }
}
