// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System;
using System.Globalization;
using System.Net;
using System.Text;
using Utils = Microsoft.Diagnostics.Monitoring.WebApi.Utilities;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class DumpFilePathTemplate
    {
        private const char PercentSpecifier = '%';
        private const char ProcessIdSpecifier = 'p';
        private const char ExecutableSpecifier = 'e';
        private const char HostSpecifier = 'h';
        private const char TimeSpecifier = 't';

        private const int PercentPosition = 0;
        private const int ProcessIdPosition = 1;
        private const int ExecutablePosition = 2;
        private const int HostPosition = 3;
        private const int TimePosition = 4;

        private readonly CompositeFormat _format;

        private DumpFilePathTemplate(CompositeFormat format)
        {
            _format = format;
        }

        public static DumpFilePathTemplate Parse(string template)
        {
            StringBuilder builder = new();
            for (int i = 0; i < template.Length; i++)
            {
                switch (template[i])
                {
                    case '{':
                        builder.Append('{');
                        goto default;
                    case '}':
                        builder.Append('}');
                        goto default;
                    case '%':
                        if (i == template.Length - 1)
                        {
                            throw new FormatException(string.Format(CultureInfo.InvariantCulture, Strings.ErrorMessage_InvalidDumpFileTemplateSpecifier, '%'));
                        }

                        char specifier = template[++i];
                        int position = specifier switch
                        {
                            PercentSpecifier => PercentPosition,
                            ProcessIdSpecifier => ProcessIdPosition,
                            ExecutableSpecifier => ExecutablePosition,
                            HostSpecifier => HostPosition,
                            TimeSpecifier => TimePosition,
                            _ => throw new FormatException(string.Format(CultureInfo.InvariantCulture, Strings.ErrorMessage_InvalidDumpFileTemplateSpecifier, "%" + specifier)),
                        };

                        builder.Append('{');
                        builder.Append(position);
                        builder.Append('}');
                        break;
                    default:
                        builder.Append(template[i]);
                        break;
                }
            }

            return new DumpFilePathTemplate(CompositeFormat.Parse(builder.ToString()));
        }

        public string ToString(IProcessInfo processInfo)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                _format,
                '%',
                processInfo.EndpointInfo.ProcessId,
                processInfo.ProcessName,
                Dns.GetHostName(),
                Utils.GetFileNameTimeStampUtcNow());
        }
    }
}
