// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using static Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing.ObjectFormatting.Formatters.DebuggerDisplay.DebuggerDisplayParser;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing.ObjectFormatting.Formatters.DebuggerDisplay
{
    internal static class DebuggerDisplayFormatter
    {
        internal record struct DebuggerDisplayAttributeValue(string Text, IList<Type> EncompassingTypes);

        public static FormatterFactoryResult? GetDebuggerDisplayFormatter(Type objType, ObjectFormatterCache? formatterCache = null)
        {
            if (objType.IsInterface)
            {
                return null;
            }

            //
            // Guard against exceptions from any of these methods as we're dealing with user-provided input.
            // None of these methods should throw but since we're potentially running inside of user code
            // we want to be safe here.
            //
            try
            {
                DebuggerDisplayAttributeValue? attribute = GetDebuggerDisplayAttribute(objType);
                if (attribute == null || attribute.Value.EncompassingTypes.Count == 0)
                {
                    return null;
                }

                //
                // We found an attribute.
                // The last encompassing type will be the source of the attribute.
                // Check if we've already processed this base type, and if so return the precomputed result.
                //
                if (formatterCache != null &&
                    formatterCache.TryGetFormatter(attribute.Value.EncompassingTypes[^1], out ObjectFormatterFunc? precachedFormatter) &&
                    precachedFormatter != null)
                {
                    return new FormatterFactoryResult(precachedFormatter, attribute.Value.EncompassingTypes);
                }

                ParsedDebuggerDisplay? parsedDebuggerDiplay = DebuggerDisplayParser.ParseDebuggerDisplay(attribute.Value.Text);
                if (parsedDebuggerDiplay == null)
                {
                    return null;
                }

                ObjectFormatterFunc? formatter = ExpressionBinder.BindParsedDebuggerDisplay(objType, parsedDebuggerDiplay.Value);
                if (formatter == null)
                {
                    return null;
                }

                return new FormatterFactoryResult(formatter, attribute.Value.EncompassingTypes);
            }
            catch
            {
                return null;
            }

        }

        internal static DebuggerDisplayAttributeValue? GetDebuggerDisplayAttribute(Type objType)
        {
            List<Type> encompassingTypes = new();

            Type? currentType = objType;
            while (currentType != null)
            {
                encompassingTypes.Add(currentType);

                try
                {
                    DebuggerDisplayAttribute? attribute = currentType.GetCustomAttributes<DebuggerDisplayAttribute>(inherit: false).FirstOrDefault();
                    if (attribute?.Value != null)
                    {
                        return new DebuggerDisplayAttributeValue(attribute.Value, encompassingTypes);
                    }
                }
                catch
                {

                }
                currentType = currentType.BaseType;
            }

            return null;
        }
    }
}
