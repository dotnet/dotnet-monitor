// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

#if STARTUPHOOK
namespace Microsoft.Diagnostics.Monitoring.StartupHook
#else
#nullable enable

namespace Microsoft.Diagnostics.Monitoring.WebApi.Stacks
#endif
{
    internal sealed class NameFormatter
    {
        public const string UnknownModule = "UnknownModule";
        public const string UnknownClass = "UnknownClass";

        private const string ArrayType = "_ArrayType_";
        private const string CompositeType = "_CompositeType_";

        private const char DotSeparator = '.';
        private const char NestedSeparator = '+';
        private const char GenericStart = '[';
        private const char GenericSeparator = ',';
        private const char GenericEnd = ']';
        private const char MethodParameterTypesStart = '(';
        private const char MethodParameterTypesEnd = ')';

        internal enum TypeFormat
        {
            Full,
            Simple
        }

        public static void BuildTypeName(StringBuilder builder, NameCache cache, FunctionData functionData)
        {
            if (functionData.ParentClass != 0)
            {
                BuildTypeName(builder, cache, functionData.ParentClass, TypeFormat.Full);
            }
            else
            {
                BuildTypeName(builder, cache, functionData.ModuleId, functionData.ParentClassToken, TypeFormat.Full);
            }
        }

        public static void BuildTypeName(StringBuilder builder, NameCache cache, ulong classId, TypeFormat typeFormat)
        {
            string typeName = UnknownClass;
            if (cache.ClassData.TryGetValue(classId, out ClassData? classData))
            {
                if (classData.Flags != ClassFlags.None)
                {
                    switch (classData.Flags)
                    {
                        case ClassFlags.Array:
                            typeName = ArrayType;
                            break;
                        case ClassFlags.Composite:
                            typeName = CompositeType;
                            break;
                        default:
                            //All other cases default to UnknownClass
                            break;
                    }

                    builder.Append(typeName);
                }
                else
                {
                    BuildTypeName(builder, cache, classData.ModuleId, classData.Token, typeFormat);
                }
                BuildGenericTypeNames(builder, cache, classData.TypeArgs, typeFormat);
            }
            else
            {
                builder.Append(typeName);
            }
        }

        private static void BuildTypeName(StringBuilder builder, NameCache cache, ulong moduleId, uint classToken, TypeFormat typeFormat)
        {
            var typeNames = new Stack<string>();

            uint currentToken = classToken;
            while (currentToken != 0 && cache.TokenData.TryGetValue(new ModuleScopedToken(moduleId, currentToken), out TokenData? tokenData))
            {
                string typeName = tokenData.Name;

                if (typeFormat == TypeFormat.Full && !string.IsNullOrEmpty(tokenData.Namespace))
                {
                    typeName = tokenData.Namespace + DotSeparator + typeName;
                }

                typeNames.Push(typeName);
                currentToken = tokenData.OuterToken;
            }

            if (typeNames.Count == 0)
            {
                builder.Append(UnknownClass);
            }

            while (typeNames.Count > 0)
            {
                string typeName = typeNames.Pop();
                builder.Append(typeName);
                if (typeNames.Count > 0)
                {
                    builder.Append(NestedSeparator);
                }
            }
        }

        public static void BuildGenericTypeNames(StringBuilder builder, NameCache cache, ulong[] parameters, TypeFormat typeFormat = TypeFormat.Full)
        {
            IList<string> typeNames = GetTypeNames(cache, parameters, typeFormat);

            BuildGenericArgTypes(builder, typeNames);
        }

        public static void BuildGenericArgTypes(StringBuilder builder, IList<string> typeNames)
        {
            WriteTypeNamesList(builder, typeNames, GenericStart, GenericEnd, GenericSeparator);
        }

        public static string RemoveGenericArgTypes(string name, out string[] genericArgTypes)
        {
            int genericsStartIndex = name.IndexOf(GenericStart);
            // Not found or an annotated frame
            if (genericsStartIndex <= 0)
            {
                genericArgTypes = [];
                return name;
            }

            int genericEndIndex = name.IndexOf(GenericEnd);
            if (genericEndIndex != name.Length - 1)
            {
                throw new InvalidOperationException("Malformed name");
            }

            genericArgTypes = name[(genericsStartIndex + 1)..genericEndIndex].Split(GenericSeparator);
            return name[..genericsStartIndex];
        }

        public static void BuildMethodParameterTypes(StringBuilder builder, IList<string> typeNames)
        {
            WriteTypeNamesList(builder, typeNames, MethodParameterTypesStart, MethodParameterTypesEnd, GenericSeparator);
        }

        private static void WriteTypeNamesList(StringBuilder builder, IList<string> typeNames, char startChar, char endChar, char separationChar)
        {
            for (int i = 0; i < typeNames?.Count; i++)
            {
                if (i == 0)
                {
                    builder.Append(startChar);
                }
                builder.Append(typeNames[i]);
                if (i < typeNames.Count - 1)
                {
                    builder.Append(separationChar);
                }
                else if (i == typeNames.Count - 1)
                {
                    builder.Append(endChar);
                }
            }
        }

        public static IList<string> GetTypeNames(NameCache cache, ulong[] types, TypeFormat typeFormat)
        {
            List<string> typesList = new();
            StringBuilder builder = new();
            for (int i = 0; i < types?.Length; i++)
            {
                builder.Clear();
                BuildTypeName(builder, cache, types[i], typeFormat);
                typesList.Add(builder.ToString());
            }

            return typesList;
        }

        public static string GetModuleName(NameCache cache, ulong moduleId)
        {
            string moduleName = UnknownModule;
            if (cache.ModuleData.TryGetValue(moduleId, out ModuleData? moduleData))
            {
                moduleName = moduleData.Name;
            }
            return moduleName;
        }
    }
}
