// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Microsoft.Diagnostics.Tools.Monitor.Extensibility
{
    internal class ExtensionException : MonitoringException
    {
        private ExtensionException(string message)
            : base(message)
        {
        }

        [DoesNotReturn]
        public static void ThrowNotFound(string extensionName)
        {
            throw new ExtensionException(string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_ExtensionNotFound, extensionName));
        }

        public static void ThrowInvalidManifest(string reason)
        {
            throw new ExtensionException(string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_ExtensionManifestInvalid, reason));
        }

        [DoesNotReturn]
        public static void ThrowLaunchFailure(string extensionName)
        {
            throw new ExtensionException(string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_ExtensionLaunchFailed, extensionName));
        }
    }
}
