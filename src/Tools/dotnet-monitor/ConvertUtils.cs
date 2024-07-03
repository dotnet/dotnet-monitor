// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;

namespace Microsoft.Diagnostics.Tools.Monitor;

internal static class ConvertUtils
{
    public static string ToString(object value, IFormatProvider? provider)
    {
        return Convert.ToString(value, provider)!; // Since value is not null this will never return null.
    }
}
