// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing.ObjectFormatting.Formatters.DebuggerDisplay;
using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing.ObjectFormatting
{
    /// <summary>
    /// The results from GetFormatter.
    /// </summary>
    /// <param name="Formatter">The object formatter.</param>
    /// <param name="MatchingTypes">Known types that this formatter will work against (including the requested type).</param>
    internal record FormatterFactoryResult(ObjectFormatterFunc Formatter, IEnumerable<Type> MatchingTypes);

    internal static class ObjectFormatterFactory
    {
        public static FormatterFactoryResult GetFormatter(Type objType, bool useDebuggerDisplayAttribute, ObjectFormatterCache? formatterCache = null)
        {
            if (useDebuggerDisplayAttribute)
            {
                FormatterFactoryResult? factoryResult = DebuggerDisplayFormatter.GetDebuggerDisplayFormatter(objType, formatterCache);
                if (factoryResult != null)
                {
                    return factoryResult;
                }
            }

            if (objType.IsAssignableTo(typeof(IConvertible)))
            {
                return new FormatterFactoryResult(RuntimeFormatters.IConvertibleFormatter, new[] { objType });
            }
            else if (objType.IsAssignableTo(typeof(IFormattable)))
            {
                return new FormatterFactoryResult(RuntimeFormatters.IFormattableFormatter, new[] { objType });
            }
            else
            {
                return new FormatterFactoryResult(RuntimeFormatters.GeneralFormatter, new[] { objType });
            }
        }
    }
}
