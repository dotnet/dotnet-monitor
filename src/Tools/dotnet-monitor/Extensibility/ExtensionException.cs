// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;

namespace Microsoft.Diagnostics.Tools.Monitor.Extensibility
{
    internal class ExtensionException : MonitoringException
    {
        private ExtensionException(string message)
            : base(message)
        {
        }

        private ExtensionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        [DoesNotReturn]
        public static void ThrowNotFound(string extensionName)
        {
            throw new ExtensionException(string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_ExtensionNotFound, extensionName));
        }

        [DoesNotReturn]
        public static void ThrowManifestNotFound(string path)
        {
            throw new ExtensionException(string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_ExtensionManifestNotFound, path));
        }

        [DoesNotReturn]
        public static void ThrowInvalidManifest(string reason)
        {
            throw new ExtensionException(string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_ExtensionManifestInvalid, reason));
        }

        [DoesNotReturn]
        public static void ThrowInvalidManifest(JsonException ex)
        {
            throw new ExtensionException(ex.Message, ex);
        }

        public static void ThrowFileNotFound(string extensionName, string path)
        {
            throw new ExtensionException(string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_ExtensionFileNotFound, path, extensionName));
        }

        [DoesNotReturn]
        public static void ThrowLaunchFailure(string extensionName)
        {
            throw new ExtensionException(string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_ExtensionLaunchFailed, extensionName));
        }
    }
}
