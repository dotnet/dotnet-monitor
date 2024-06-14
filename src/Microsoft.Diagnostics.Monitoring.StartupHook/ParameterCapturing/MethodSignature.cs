// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing
{
    internal sealed class MethodSignature(MethodInfo method)
    {
        private static class Tokens
        {
            private static class Internal
            {
                public const string Prefix = "<";
                public const string Postfix = ">";
            }

            public static class Types
            {
                public const char ArityDelimiter = '`';
            }

            public static class Generics
            {
                public const char Start = '<';
                public const string Separator = ", ";
                public const char End = '>';
            }

            public static class Parameters
            {
                public static class Names
                {
                    public const string ImplicitThis = "this";
                    public const string Unknown = Internal.Prefix + "unknown" + Internal.Postfix;
                }
            }
        }

        public string ModuleName { get; } = method.Module.Name;

        public string? TypeName { get; } = EmitTypeName(method.DeclaringType);

        public string MethodName { get; } = GetMethodName(method);

        public IReadOnlyList<ParameterSignature> Parameters { get; } = EmitParameters(method);

        private static string GetMethodName(MethodInfo method)
        {
            StringBuilder builder = new();

            builder.Append(method.Name);
            EmitGenericArguments(builder, method.GetGenericArguments());

            return builder.ToString();
        }

        private static string? EmitTypeName(Type? type)
        {
            // For a generic declaring type, trim the arity information and replace it with the known generic argument names.
            string? declaringTypeName = type?.FullName?.Split(Tokens.Types.ArityDelimiter)?[0];
            if (declaringTypeName is null)
            {
                return null;
            }

            StringBuilder builder = new();
            builder.Append(declaringTypeName);
            EmitGenericArguments(builder, type?.GetGenericArguments());

            return builder.ToString();
        }

        private static List<ParameterSignature> EmitParameters(MethodInfo method)
        {
            ParameterInfo[] explicitParameters = method.GetParameters();

            List<ParameterSignature> parameters = new(explicitParameters.Length + 1);

            if (method.HasImplicitThis())
            {
                parameters.Add(new ParameterSignature(
                    Name: Tokens.Parameters.Names.ImplicitThis,
                    Type: EmitTypeName(method.DeclaringType),
                    TypeModuleName: method.DeclaringType?.Module.Name,
                    ParameterAttributes.None,
                    IsByRef: false));
            }

            foreach (ParameterInfo paramInfo in explicitParameters)
            {
                parameters.Add(new ParameterSignature(
                    Name: paramInfo.Name,
                    Type: EmitTypeName(paramInfo.ParameterType),
                    TypeModuleName: paramInfo.ParameterType.Module.Name,
                    paramInfo.Attributes,
                    paramInfo.ParameterType.IsByRef || paramInfo.ParameterType.IsByRefLike));
            }

            return parameters;
        }

        private static void EmitGenericArguments(StringBuilder stringBuilder, Type[]? genericArgs)
        {
            if (genericArgs == null || genericArgs.Length == 0)
            {
                return;
            }

            stringBuilder.Append(Tokens.Generics.Start);
            for (int i = 0; i < genericArgs.Length; i++)
            {
                if (i != 0)
                {
                    stringBuilder.Append(Tokens.Generics.Separator);
                }

                stringBuilder.Append(genericArgs[i].Name);
            }
            stringBuilder.Append(Tokens.Generics.End);
        }
    }
}
