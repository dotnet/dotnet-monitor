// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Microsoft.Diagnostics.Monitoring.Options
{
    internal static class OptionUtils
    {
        public static void ThrowIfNotConfigured<T>([DoesNotReturnIf(false)] bool IsValid)
        {
            if (!IsValid)
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.InvariantCulture,
                    OptionsDisplayStrings.ErrorMessage_OptionNotConfigured,
                    nameof(T)));
            }
        }
    }
}
