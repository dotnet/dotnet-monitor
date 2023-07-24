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

        public static void BuildClassName(StringBuilder builder, NameCache cache, FunctionData functionData)
        {
            if (functionData.ParentClass != 0)
            {
                BuildClassName(builder, cache, functionData.ParentClass);
            }
            else
            {
                BuildClassName(builder, cache, functionData.ModuleId, functionData.ParentToken);
            }
        }

        public static void BuildClassName(StringBuilder builder, NameCache cache, ulong classId, bool friendlyNames = false)
        {
            string className = UnknownClass;
            if (cache.ClassData.TryGetValue(classId, out ClassData? classData))
            {
                if (classData.Flags != ClassFlags.None)
                {
                    switch (classData.Flags)
                    {
                        case ClassFlags.Array:
                            className = ArrayType;
                            break;
                        case ClassFlags.Composite:
                            className = CompositeType;
                            break;
                        default:
                            //All other cases default to UnknownClass
                            break;
                    }

                    builder.Append(className);
                }
                else
                {
                    BuildClassName(builder, cache, classData.ModuleId, classData.Token, friendlyNames: friendlyNames);
                }
                BuildGenericParameters(builder, cache, classData.TypeArgs);
            }
            else
            {
                builder.Append(className);
            }
        }

        private static void BuildClassName(StringBuilder builder, NameCache cache, ulong moduleId, uint token, bool friendlyNames = false)
        {
            var classNames = new Stack<string>();

            uint currentToken = token;
            while (currentToken != 0 && cache.TokenData.TryGetValue(new ModuleScopedToken(moduleId, currentToken), out TokenData? tokenData))
            {
                classNames.Push(friendlyNames ? tokenData.FriendlyName : tokenData.Name);
                currentToken = tokenData.OuterToken;
            }

            if (classNames.Count == 0)
            {
                builder.Append(UnknownClass);
            }

            while (classNames.Count > 0)
            {
                string className = classNames.Pop();
                builder.Append(className);
                if (classNames.Count > 0)
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
                BuildClassName(builder, cache, parameters[i]);
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

        public static IList<string> GetMethodParameterTypes(StringBuilder builder, NameCache cache, ulong[] parameterTypes, bool friendlyNames = false)
        {
            List<string> parameterTypesList = new();
            for (int i = 0; i < parameterTypes?.Length; i++)
            {
                builder.Clear();
                BuildClassName(builder, cache, parameterTypes[i], friendlyNames: friendlyNames);
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
