// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Microsoft.Diagnostics.Monitoring;

namespace Microsoft.Diagnostics.Tools.Monitor.Extensibility
{
    internal class ExtensionLaunchFailure : MonitoringException
    {
        public ExtensionLaunchFailure(string extensionName)
            : base(string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_ExtensionLaunchFailed, extensionName))
        {
        }
    }
}
