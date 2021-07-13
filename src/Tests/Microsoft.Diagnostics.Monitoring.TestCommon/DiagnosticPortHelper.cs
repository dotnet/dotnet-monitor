// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.UnitTests.Options;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Monitoring.UnitTests
{
    public static class DiagnosticPortHelper
    {
        /// <summary>
        /// Calculates the app's diagnostic port mode and generates a port path
        /// if <paramref name="monitorConnectionMode"/> is <see cref="DiagnosticPortConnectionMode.Listen"/>.
        /// </summary>
        public static void Generate(
            Options.DiagnosticPortConnectionMode monitorConnectionMode,
            out DiagnosticPortConnectionMode appConnectionMode,
            out string diagnosticPortPath)
        {
            appConnectionMode = DiagnosticPortConnectionMode.Listen;
            diagnosticPortPath = null;

            if (DiagnosticPortConnectionMode.Listen == monitorConnectionMode)
            {
                appConnectionMode = DiagnosticPortConnectionMode.Connect;

                string fileName = Guid.NewGuid().ToString("D");
                diagnosticPortPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                    fileName : Path.Combine(Path.GetTempPath(), fileName);
            }
        }
    }
}
