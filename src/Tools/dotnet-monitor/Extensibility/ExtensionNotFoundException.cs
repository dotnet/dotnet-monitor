// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.Diagnostics.Monitoring;

namespace Microsoft.Diagnostics.Tools.Monitor.Extensibility
{
    internal class ExtensionException : MonitoringException
    {
        private ExtensionException(string message)
            : base(message)
        {
        }

        [DoesNotReturn]
        public static ExtensionException ThrowNotFound(string extensionName)
        {
            throw new ExtensionException(string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_ExtensionNotFound, extensionName));
        }

        [DoesNotReturn]
        public static ExtensionException ThrowLaunchFailure(string extensionName)
        {
            throw new ExtensionException(string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_ExtensionLaunchFailed, extensionName));
        }

        [DoesNotReturn]
        public static ExtensionException ThrowWrongType(string extensionName, string extensionPath, Type requiredType)
        {
            throw new ExtensionException(string.Format(CultureInfo.CurrentCulture, Strings.LogFormatString_ExtensionNotOfType, extensionName, extensionPath, requiredType.Name));
        }
    }
}
