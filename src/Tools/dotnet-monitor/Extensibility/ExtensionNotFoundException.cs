// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;

namespace Microsoft.Diagnostics.Tools.Monitor.Extensibility
{
    public class ExtensionNotFoundException : Exception
    {
        public ExtensionNotFoundException(string extensionMoniker)
            : base(string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_ExtensionNotFound, extensionMoniker))
        {
        }
    }
}
