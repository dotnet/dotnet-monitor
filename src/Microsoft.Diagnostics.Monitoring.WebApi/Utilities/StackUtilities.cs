// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal static class StackUtilities
    {
        public static string GenerateStacksFilename(IEndpointInfo endpointInfo, bool plainText)
        {
            string extension = plainText ? "txt" : "json";
            return FormattableString.Invariant($"{Utilities.GetFileNameTimeStampUtcNow()}_{endpointInfo.ProcessId}.stacks.{extension}");
        }
    }
}
