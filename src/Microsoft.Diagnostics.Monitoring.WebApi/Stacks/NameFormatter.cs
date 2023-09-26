// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        private const char NestedSeparator = '+';
        private const char GenericStart = '[';
        private const char GenericSeparator = ',';
        private const char GenericEnd = ']';

        public static void BuildTypeName(StringBuilder builder, NameCache cache, FunctionData functionData)
        {
            if (functionData.ParentClass != 0)
            {
                BuildTypeName(builder, cache, functionData.ParentClass);
            }
            else
            {
                BuildTypeName(builder, cache, functionData.ModuleId, functionData.ParentToken);
            }
        }

        public static void BuildTypeName(StringBuilder builder, NameCache cache, ulong classId)
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
                    BuildTypeName(builder, cache, classData.ModuleId, classData.Token);
                }
                BuildGenericParameters(builder, cache, classData.TypeArgs);
            }
            else
            {
                builder.Append(typeName);
            }
        }

        private static void BuildTypeName(StringBuilder builder, NameCache cache, ulong moduleId, uint token)
        {
            var typeNames = new Stack<string>();

            uint currentToken = token;
            while (currentToken != 0 && cache.TokenData.TryGetValue(new ModuleScopedToken(moduleId, currentToken), out TokenData? tokenData))
            {
                typeNames.Push(tokenData.Name);
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

        public static void BuildGenericParameters(StringBuilder builder, NameCache cache, ulong[] parameters)
        {
            for (int i = 0; i < parameters?.Length; i++)
            {
                if (i == 0)
                {
                    builder.Append(GenericStart);
                }
                BuildTypeName(builder, cache, parameters[i]);
                if (i < parameters.Length - 1)
                {
                    builder.Append(GenericSeparator);
                }
                else if (i == parameters.Length - 1)
                {
                    builder.Append(GenericEnd);
                }
            }
        }

        public static IList<string> GetMethodParameterTypes(StringBuilder builder, NameCache cache, ulong[] parameterTypes)
        {
            List<string> parameterTypesList = new();
            for (int i = 0; i < parameterTypes?.Length; i++)
            {
                builder.Clear();
                BuildTypeName(builder, cache, parameterTypes[i]);
                parameterTypesList.Add(builder.ToString());
            }

            return parameterTypesList;
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
