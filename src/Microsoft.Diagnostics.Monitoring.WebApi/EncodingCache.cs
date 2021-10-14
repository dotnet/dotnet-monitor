// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal static class EncodingCache
    {
        public static readonly Encoding UTF8NoBOM =
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
    }
}
