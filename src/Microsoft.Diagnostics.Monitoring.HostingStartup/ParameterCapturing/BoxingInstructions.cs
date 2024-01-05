// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.FunctionProbes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing
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

        public static ParameterBoxingInstructions[] GetBoxingInstructions(MethodInfo method)
        {
            List<Type> methodParameterTypes = method.GetParameters().Select(p => p.ParameterType).ToList();
            List<ParameterBoxingInstructions> instructions = new(methodParameterTypes.Count + (method.HasImplicitThis() ? 1 : 0));
            //
            // A signature decoder will used to determine boxing tokens for parameter types that cannot be determined from standard
            // reflection alone. The boxing tokens generated from this decoder should only be used to fill in these gaps
            // as it is not a comprehensive decoder and will produce an unsupported boxing instruction for any types not explicitly mentioned
            // in BoxingTokensSignatureProvider's summary.
            // 
            Lazy<ParameterBoxingInstructions[]?> ancillaryInstructions = new(() => GetAncillaryBoxingInstructionsFromMethodSignature(method));

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
                    instructions.Add(SpecialCaseBoxingTypes.Unknown);
                }
                else
                {
                    methodParameterTypes.Insert(0, thisType);
                    // Implicit this isn't a formal parameter, so offset by one.
                    formalParameterPosition = -1;
                }
            }

            foreach (Type paramType in methodParameterTypes)
            {
                if (paramType.IsByRef ||
                    paramType.IsByRefLike ||
                    paramType.IsPointer)
                {
                    instructions.Add(SpecialCaseBoxingTypes.Unknown);
                }
                else if (paramType.IsGenericParameter)
                {
                    instructions.Add(SpecialCaseBoxingTypes.Unknown);
                }
                else if (paramType.IsPrimitive)
                {
                    instructions.Add(GetSpecialCaseBoxingTokenForPrimitive(paramType));
                }
                else if (paramType.IsValueType)
                {
                    // Ref structs have already been filtered out by the above IsByRefLike check.
                    if (paramType.IsGenericType ||
                        paramType.Assembly != method.Module.Assembly)
                    {
                        // Typeref or Typespec
                        if (formalParameterPosition >= 0)
                        {
                            ParameterBoxingInstructions? signatureDecoderInstructions = ancillaryInstructions.Value?[formalParameterPosition];
                            // value-type type refs are supported by the signature decoder
                            if (signatureDecoderInstructions.HasValue)
                            {
                                ParameterBoxingInstructions unwrappedInstructions = signatureDecoderInstructions.Value;
                                if (paramType.IsGenericType)
                                {
                                    unwrappedInstructions.InstructionType = InstructionType.TypeSpec;
                                }

                                instructions.Add(unwrappedInstructions);
                            }
                            else
                            {
                                instructions.Add(SpecialCaseBoxingTypes.Unknown);
                            }

                        }
                        else
                        {
                            instructions.Add(SpecialCaseBoxingTypes.Unknown);
                        }
                    }
                    else
                    {
                        // Typedef
                        instructions.Add((uint)paramType.MetadataToken);
                    }
                }
                else if (paramType.IsArray ||
                    paramType.IsClass ||
                    paramType.IsInterface)
                {
                    instructions.Add(SpecialCaseBoxingTypes.Object);
                }
                else
                {
                    instructions.Add(SpecialCaseBoxingTypes.Unknown);
                }

                formalParameterPosition++;
            }

            return instructions.ToArray();
        }

        private static unsafe ParameterBoxingInstructions[]? GetAncillaryBoxingInstructionsFromMethodSignature(MethodInfo method)
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


                // TODO: Comment this is the expanded helper method
                BlobReader blobReader = mdReader.GetBlobReader(methodDef.Signature);
                SignatureDecoder<ParameterBoxingInstructions, object?> signatureDecoder = new(new BoxingTokensSignatureProvider(), mdReader, genericContext: null);

                MethodSignature<ParameterBoxingInstructions> methodSignature = SignatureDecoderEx.DecodeMethodSignature(signatureDecoder, ref blobReader);

                return [.. methodSignature.ParameterTypes];
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
