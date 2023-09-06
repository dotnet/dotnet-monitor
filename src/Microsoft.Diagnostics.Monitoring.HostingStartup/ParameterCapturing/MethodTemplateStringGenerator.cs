// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Text;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing
{
    internal static class MethodTemplateStringGenerator
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
                public const char Separator = '.';
                public const string Unknown = Internal.Prefix + "unknown" + Internal.Postfix;
            }

            public static class Parameters
            {
                public const char Start = '(';
                public const string Separator = ", ";
                public const string NameValueSeparator = ": ";
                public const char End = ')';

                public static class Modifiers
                {
                    public const char Separator = ' ';
                    public const string RefOrRefLike = "ref";
                    public const string In = "in";
                    public const string Out = "out";
                }

                public static class Names
                {
                    public const string ImplicitThis = "this";
                    public const string Unknown = Internal.Prefix + "unknown" + Internal.Postfix;
                }

                public static class Values
                {
                    //
                    // TODO: Move this to to ObjectFormatter. Currently unsupported values are pre-baked into the template string.
                    // However this means unsupported parameters won't correctly show up in structured logging.
                    // Moving this to ObjectFormatter will fix this.
                    //
                    public const string Unsupported = Internal.Prefix + "unsupported" + Internal.Postfix;
                }
            }

            public static class Generics
            {
                public const char Start = '<';
                public const string Separator = ", ";
                public const char End = '>';
            }
        }

        public static string GenerateTemplateString(MethodInfo method, bool[] supportedParameters)
        {
            StringBuilder fmtStringBuilder = new();

            // Declaring type name
            // For a generic declaring type, trim the arity information and replace it with the known generic argument names.
            string declaringTypeName = method.DeclaringType?.FullName?.Split(Tokens.Types.ArityDelimiter)?[0] ?? Tokens.Types.Unknown;
            fmtStringBuilder.Append(declaringTypeName);
            EmitGenericArguments(fmtStringBuilder, method.DeclaringType?.GetGenericArguments());

            // Method name
            if (fmtStringBuilder.Length != 0)
            {
                fmtStringBuilder.Append(Tokens.Types.Separator);
            }
            fmtStringBuilder.Append(method.Name);
            EmitGenericArguments(fmtStringBuilder, method.GetGenericArguments());

            // Method parameters
            fmtStringBuilder.Append(Tokens.Parameters.Start);

            int parameterIndex = 0;
            ParameterInfo[] explicitParameters = method.GetParameters();

            int numberOfParameters = explicitParameters.Length + (method.HasImplicitThis() ? 1 : 0);
            if (numberOfParameters != supportedParameters.Length)
            {
                throw new ArgumentException(nameof(supportedParameters));
            }

            // Implicit this
            if (method.HasImplicitThis())
            {
                EmitParameter(
                    fmtStringBuilder,
                    method.DeclaringType,
                    Tokens.Parameters.Names.ImplicitThis,
                    supportedParameters[parameterIndex]);
                parameterIndex++;
            }

            foreach (ParameterInfo paramInfo in explicitParameters)
            {
                if (parameterIndex != 0)
                {
                    fmtStringBuilder.Append(Tokens.Parameters.Separator);
                }

                string name = paramInfo.Name ?? Tokens.Parameters.Names.Unknown;
                EmitParameter(
                    fmtStringBuilder,
                    paramInfo.ParameterType,
                    name,
                    supportedParameters[parameterIndex],
                    paramInfo);

                parameterIndex++;
            }

            fmtStringBuilder.Append(Tokens.Parameters.End);

            return fmtStringBuilder.ToString();
        }

        private static void EmitParameter(StringBuilder stringBuilder, Type? type, string name, bool isSupported, ParameterInfo? paramInfo = null)
        {
            stringBuilder.AppendLine();
            stringBuilder.Append('\t');

            // Modifiers
            if (paramInfo?.IsIn == true)
            {
                stringBuilder.Append(Tokens.Parameters.Modifiers.In);
                stringBuilder.Append(Tokens.Parameters.Modifiers.Separator);
            }
            else if (paramInfo?.IsOut == true)
            {
                stringBuilder.Append(Tokens.Parameters.Modifiers.Out);
                stringBuilder.Append(Tokens.Parameters.Modifiers.Separator);
            }
            else if (type?.IsByRef == true ||
                    type?.IsByRefLike == true)
            {
                stringBuilder.Append(Tokens.Parameters.Modifiers.RefOrRefLike);
                stringBuilder.Append(Tokens.Parameters.Modifiers.Separator);
            }

            // Name
            stringBuilder.Append(name);
            stringBuilder.Append(Tokens.Parameters.NameValueSeparator);

            // Value (format item or unsupported)
            if (isSupported)
            {
                EmitFormatItem(stringBuilder, name);
            }
            else
            {
                stringBuilder.Append(Tokens.Parameters.Values.Unsupported);
            }
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

        private static void EmitFormatItem(StringBuilder stringBuilder, string name)
        {
            stringBuilder.Append('{');
            stringBuilder.Append(name);
            stringBuilder.Append('}');
        }
    }
}
