// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal static class EncodingCache
    {
        // Encode UTF8 without BOM and write "?" as fallback replacement.
        public static readonly Encoding UTF8NoBOMNoThrow =
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    }
}
