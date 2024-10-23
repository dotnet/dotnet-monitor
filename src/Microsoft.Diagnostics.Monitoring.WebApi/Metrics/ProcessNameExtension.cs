// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal static class ProcessNameExtension
    {
        private const string IisWorkerProcessName = "w3wp";
        internal static string GetProcessName(this IProcessInfo process)
        {
            if (process.ProcessName != IisWorkerProcessName)
            {
                return process.ProcessName;
            }

            //for iis let's use app pool name
            return GetIisAppPoolName(process.CommandLine);
        }

        private static readonly char[] AppPoolTrimChars = new[] { '\\', '"' };
        private static string GetIisAppPoolName(string? commandLine)
        {
            if (string.IsNullOrWhiteSpace(commandLine))
            {
                return IisWorkerProcessName;
            }

            string[] parts = commandLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            int apParamIdx = Array.IndexOf(parts, "-ap");
            if (apParamIdx != -1 && apParamIdx < parts.Length - 1)
            {
                return parts[apParamIdx + 1].Trim(AppPoolTrimChars);
            }

            return IisWorkerProcessName;
        }
    }
}
