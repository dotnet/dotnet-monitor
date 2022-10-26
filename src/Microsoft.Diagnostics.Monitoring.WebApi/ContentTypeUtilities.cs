// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Net.Http.Headers;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal static class ContentTypeUtilities
    {
        public static readonly MediaTypeHeaderValue ApplicationJsonHeader = new MediaTypeHeaderValue(ContentTypes.ApplicationJson);
        public static readonly MediaTypeHeaderValue NdJsonHeader = new MediaTypeHeaderValue(ContentTypes.ApplicationNdJson);
        public static readonly MediaTypeHeaderValue JsonSequenceHeader = new MediaTypeHeaderValue(ContentTypes.ApplicationJsonSequence);
        public static readonly MediaTypeHeaderValue TextPlainHeader = new MediaTypeHeaderValue(ContentTypes.TextPlain);
        public static readonly MediaTypeHeaderValue SpeedScopeJsonHeader = new MediaTypeHeaderValue(ContentTypes.ApplicationSpeedscopeJson);

        public static StackFormat? ComputeStackFormat(IList<MediaTypeHeaderValue> acceptedHeaders)
        {
            // CONSIDER We currently don't factor quality into account.
            // CONSIDER Centralize content negotiation across logs, stacks, and any other types that need it.

            if (acceptedHeaders == null) return null;
            if (acceptedHeaders.Contains(TextPlainHeader)) return StackFormat.PlainText;
            if (acceptedHeaders.Contains(ApplicationJsonHeader)) return StackFormat.Json;
            if (acceptedHeaders.Contains(SpeedScopeJsonHeader)) return StackFormat.Speedscope;
            if (acceptedHeaders.Any(TextPlainHeader.IsSubsetOf)) return StackFormat.PlainText;
            if (acceptedHeaders.Any(ApplicationJsonHeader.IsSubsetOf)) return StackFormat.Json;
            if (acceptedHeaders.Any(SpeedScopeJsonHeader.IsSubsetOf)) return StackFormat.Speedscope;
            return null;
        }

        public static bool IsPlainText(StackFormat format) => format == StackFormat.PlainText;

        public static string MapFormatToContentType(StackFormat stackFormat) =>
            stackFormat switch
            {
                StackFormat.PlainText => ContentTypes.TextPlain,
                StackFormat.Json => ContentTypes.ApplicationJson,
                StackFormat.Speedscope => ContentTypes.ApplicationSpeedscopeJson,
                _ => null
            };
    }
}
