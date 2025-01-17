// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Net.Http.Headers;
using System;
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
        public static readonly MediaTypeHeaderValue SpeedscopeJsonHeader = new MediaTypeHeaderValue(ContentTypes.ApplicationSpeedscopeJson);

        public static StackFormat? ComputeStackFormat(IList<MediaTypeHeaderValue> acceptedHeaders)
        {
            // CONSIDER We currently don't factor quality into account.
            // CONSIDER Centralize content negotiation across logs, stacks, and any other types that need it.

            if (acceptedHeaders == null) return null;
            if (acceptedHeaders.Contains(TextPlainHeader)) return StackFormat.PlainText;
            if (acceptedHeaders.Contains(ApplicationJsonHeader)) return StackFormat.Json;
            if (acceptedHeaders.Contains(SpeedscopeJsonHeader)) return StackFormat.Speedscope;
            if (acceptedHeaders.Any(TextPlainHeader.IsSubsetOf)) return StackFormat.PlainText;
            if (acceptedHeaders.Any(ApplicationJsonHeader.IsSubsetOf)) return StackFormat.Json;
            if (acceptedHeaders.Any(SpeedscopeJsonHeader.IsSubsetOf)) return StackFormat.Speedscope;
            return null;
        }

        public static string MapFormatToContentType(StackFormat stackFormat) =>
            stackFormat switch
            {
                StackFormat.PlainText => ContentTypes.TextPlain,
                StackFormat.Json => ContentTypes.ApplicationJson,
                StackFormat.Speedscope => ContentTypes.ApplicationSpeedscopeJson,
                _ => throw new InvalidOperationException()
            };

        public static string MapFormatToContentType(ExceptionFormat exceptionsFormat) =>
            exceptionsFormat switch
            {
                ExceptionFormat.PlainText => ContentTypes.TextPlain,
                ExceptionFormat.JsonSequence => ContentTypes.ApplicationJsonSequence,
                ExceptionFormat.NewlineDelimitedJson => ContentTypes.ApplicationNdJson,
                _ => throw new InvalidOperationException()
            };

        public static CapturedParameterFormat? ComputeCapturedParameterFormat(IList<MediaTypeHeaderValue> acceptedHeaders)
        {
            // CONSIDER We currently don't factor quality into account.
            // CONSIDER Centralize content negotiation across logs, stacks, and any other types that need it.

            if (acceptedHeaders == null) return null;
            if (acceptedHeaders.Contains(TextPlainHeader)) return CapturedParameterFormat.PlainText;
            if (acceptedHeaders.Contains(JsonSequenceHeader)) return CapturedParameterFormat.JsonSequence;
            if (acceptedHeaders.Contains(NdJsonHeader)) return CapturedParameterFormat.NewlineDelimitedJson;
            if (acceptedHeaders.Any(TextPlainHeader.IsSubsetOf)) return CapturedParameterFormat.PlainText;
            if (acceptedHeaders.Any(JsonSequenceHeader.IsSubsetOf)) return CapturedParameterFormat.JsonSequence;
            if (acceptedHeaders.Any(NdJsonHeader.IsSubsetOf)) return CapturedParameterFormat.NewlineDelimitedJson;
            return null;
        }
    }
}
