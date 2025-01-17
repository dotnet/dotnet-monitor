// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.StartupHook.MonitorMessageDispatcher.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing
{
    internal sealed class DeniedMethodsException : ArgumentException
    {
        public DeniedMethodsException(MethodDescription methodDescription) : this(new MethodDescription[] { methodDescription }) { }

        public DeniedMethodsException(IEnumerable<MethodDescription> deniedMethods) : base(BuildMessage(deniedMethods))
        {
        }

        private static string BuildMessage(IEnumerable<MethodDescription> deniedMethods)
        {
            StringBuilder text = new();
            text.AppendLine();
            foreach (MethodDescription method in deniedMethods)
            {
                text.Append("--> ");
                text.AppendLine(method.ToString());
            }

            return string.Format(CultureInfo.InvariantCulture, ParameterCapturingStrings.DeniedMethodsFormatString, text.ToString());
        }
    }
}
