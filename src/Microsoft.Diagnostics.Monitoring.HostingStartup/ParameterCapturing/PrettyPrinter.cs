// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Text;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing
{
    internal static class PrettyPrinter
    {
        public static string FormatObject(object value)
        {
            if (value == null)
            {
                return ParameterCapturingStrings.NullArgumentValue;
            }

            try
            {
                bool doWrapValue = false;
                string serializedValue;

                //
                // TODO: Consider memoizing (when possible) which serialization path should be taken
                // for each parameter and storing it in the method cache if this needs to be more performant
                // as more options are added.
                //
                if (value is IConvertible ic)
                {
                    serializedValue = ic.ToString(provider: null);
                    doWrapValue = (value is string);
                }
                else if (value is IFormattable formattable)
                {
                    serializedValue = formattable.ToString(format: null, formatProvider: null);
                    doWrapValue = true;
                }
                else
                {
                    serializedValue = value.ToString() ?? string.Empty;
                    doWrapValue = true;
                }

                return doWrapValue ? string.Concat('\'', serializedValue, '\'') : serializedValue;
            }
            catch
            {
                return ParameterCapturingStrings.UnknownArgumentValue;
            }
        }

        public static string? ConstructFormattableStringFromMethod(MethodInfo method, bool[] supportedParameters)
        {
            StringBuilder fmtStringBuilder = new();

            // Declaring type name
            // For a generic declaring type, trim the arity information and replace it with the known generic argument names.
            const char arityDelimiter = '`';
            string className = method.DeclaringType?.FullName?.Split(arityDelimiter)?[0] ?? string.Empty;
            fmtStringBuilder.Append(className);
            EmitGenericArguments(fmtStringBuilder, method.DeclaringType?.GetGenericArguments());

            // Method name
            if (fmtStringBuilder.Length != 0)
            {
                fmtStringBuilder.Append('.');
            }
            fmtStringBuilder.Append(method.Name);
            EmitGenericArguments(fmtStringBuilder, method.GetGenericArguments());

            // Method parameters
            fmtStringBuilder.Append('(');

            int fmtIndex = 0;
            int parameterIndex = 0;
            ParameterInfo[] explicitParameters = method.GetParameters();

            int numberOfParameters = explicitParameters.Length + (method.HasImplicitThis() ? 1 : 0);
            if (numberOfParameters != supportedParameters.Length)
            {
                return null;
            }

            // Implicit this
            if (method.HasImplicitThis())
            {
                if (EmitParameter(
                    fmtStringBuilder,
                    method.DeclaringType,
                    ParameterCapturingStrings.ThisParameterName,
                    supportedParameters[parameterIndex],
                    fmtIndex))
                {
                    fmtIndex++;
                }
                parameterIndex++;
            }

            foreach (ParameterInfo paramInfo in explicitParameters)
            {
                if (parameterIndex != 0 || fmtIndex != 0)
                {
                    fmtStringBuilder.Append(", ");
                }

                if (EmitParameter(
                    fmtStringBuilder,
                    paramInfo.ParameterType,
                    paramInfo.Name,
                    supportedParameters[parameterIndex],
                    fmtIndex,
                    paramInfo))
                {
                    fmtIndex++;
                }

                parameterIndex++;
            }

            fmtStringBuilder.Append(')');

            return fmtStringBuilder.ToString();
        }

        private static bool EmitParameter(StringBuilder stringBuilder, Type? type, string? name, bool isSupported, int formatIndex, ParameterInfo? paramInfo = null)
        {
            stringBuilder.AppendLine();
            stringBuilder.Append('\t');

            // Modifiers
            if (paramInfo?.IsIn == true)
            {
                stringBuilder.Append(ParameterCapturingStrings.ParameterModifier_In);
                stringBuilder.Append(' ');
            }
            else if (paramInfo?.IsOut == true)
            {
                stringBuilder.Append(ParameterCapturingStrings.ParameterModifier_Out);
                stringBuilder.Append(' ');
            }
            else if (type?.IsByRef == true ||
                    type?.IsByRefLike == true)
            {
                stringBuilder.Append(ParameterCapturingStrings.ParameterModifier_RefOrRefLike);
                stringBuilder.Append(' ');
            }

            // Name
            if (string.IsNullOrEmpty(name))
            {
                EmitReservedPlaceholder(stringBuilder, ParameterCapturingStrings.UnknownParameterName);
            }
            else
            {
                stringBuilder.Append(name);
            }

            stringBuilder.Append(": ");

            // Value (format item or unsupported)
            if (isSupported)
            {
                stringBuilder.Append('{');
                stringBuilder.Append(formatIndex);
                stringBuilder.Append('}');
                return true;
            }
            else
            {
                EmitReservedPlaceholder(stringBuilder, ParameterCapturingStrings.UnsupportedParameter);
                return false;
            }
        }

        private static void EmitGenericArguments(StringBuilder stringBuilder, Type[]? genericArgs)
        {
            if (genericArgs == null || genericArgs.Length == 0)
            {
                return;
            }

            stringBuilder.Append('<');
            for (int i = 0; i < genericArgs.Length; i++)
            {
                if (i != 0)
                {
                    stringBuilder.Append(", ");
                }

                stringBuilder.Append(genericArgs[i].Name);
            }
            stringBuilder.Append('>');
        }

        private static void EmitReservedPlaceholder(StringBuilder stringBuilder, string value)
        {
            stringBuilder.Append("{{");
            stringBuilder.Append(value);
            stringBuilder.Append("}}");
        }
    }
}
