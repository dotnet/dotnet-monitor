// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Globalization;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    [TypeConverter(typeof(ProcessKeyTypeConverter))]
    internal struct ProcessKey
    {
        internal ProcessKey(int processId)
        {
            ProcessId = processId;
            ProcessName = null;
            RuntimeInstanceCookie = null;
        }

        internal ProcessKey(Guid runtimeInstanceCookie)
        {
            ProcessId = null;
            ProcessName = null;
            RuntimeInstanceCookie = runtimeInstanceCookie;
        }

        internal ProcessKey(string processName)
        {
            ProcessId = null;
            ProcessName = processName;
            RuntimeInstanceCookie = null;
        }

        internal ProcessKey(int? processId = null, Guid? runtimeInstanceCookie = null, string processName = null)
        {
            ProcessId = processId;
            ProcessName = processName;
            RuntimeInstanceCookie = runtimeInstanceCookie;
        }

        internal int? ProcessId { get; }

        internal string ProcessName { get; }

        internal Guid? RuntimeInstanceCookie { get; }
    }

    internal class ProcessKeyTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (null == sourceType)
            {
                throw new ArgumentNullException(nameof(sourceType));
            }
            return sourceType == typeof(string) || sourceType == typeof(ProcessKey);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string valueString)
            {
                if (string.IsNullOrEmpty(valueString))
                {
                    return null;
                }
                else if (Guid.TryParse(valueString, out Guid cookie))
                {
                    return new ProcessKey(cookie);
                }
                else if (int.TryParse(valueString, out int processId))
                {
                    return new ProcessKey(processId);
                }
                return new ProcessKey(valueString);
            }
            else if (value is ProcessKey identifier)
            {
                return identifier;
            }
            throw new FormatException();
        }
    }
}
