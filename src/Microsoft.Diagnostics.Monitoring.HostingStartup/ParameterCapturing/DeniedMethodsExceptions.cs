// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.StartupHook.MonitorMessageDispatcher.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing
{
    internal sealed class DeniedMethodsExceptions : ArgumentException
    {

        public DeniedMethodsExceptions(MethodDescription methodDescription) : this(new MethodDescription[] { methodDescription }) { }

        public DeniedMethodsExceptions(IEnumerable<MethodDescription> unresolvedMethods) : base(BuildMessage(unresolvedMethods))
        {
        }

        private static string BuildMessage(IEnumerable<MethodDescription> unresolvedMethods)
        {
            StringBuilder text = new();
            text.AppendLine();
            foreach (MethodDescription method in unresolvedMethods)
            {
                text.Append("--> ");
                text.AppendLine(method.ToString());
            }

            return string.Format(CultureInfo.InvariantCulture, ParameterCapturingStrings.UnresolvedMethodsFormatString, text.ToString());
        }
    }
}
