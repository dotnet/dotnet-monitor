// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress
{
    /// <summary>
    /// Exception that egress providers can throw when an operational error occurs (e.g. failed to write the stream data).
    /// </summary>
    internal class Utilities
    {
        internal static void WriteInfoLogs(string logMessage, string[] args)
        {
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, logMessage, args));
        }

        internal static void WriteWarningLogs(string logMessage, string[] args)
        {
            Console.Error.WriteLine(string.Format(CultureInfo.InvariantCulture, logMessage, args));
        }
    }
}
