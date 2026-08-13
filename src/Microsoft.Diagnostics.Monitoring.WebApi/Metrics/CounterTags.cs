// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    /// <summary>
    /// Recovers the original key/value pairs from the tag strings carried on a counter payload.
    /// </summary>
    internal static class CounterTags
    {
        // System.Diagnostics.Metrics tags reach dotnet-monitor in a canonical escaped form
        // ('\' -> '\\', ',' -> '\,', '=' -> '\=') so a ',' or '=' inside a key or value stays
        // distinguishable from the ',' separating pairs and the '=' separating a key from its value.
        // That form is a transport detail and must be decoded before tags are exposed to a caller.
        // EventCounters metadata predates the encoding and uses unescaped ':'-separated pairs, so the
        // two shapes cannot share a parser.
        public static bool IsMeterPayload(ICounterPayload payload) =>
            payload switch
            {
                GaugePayload or PercentilePayload or CounterEndedPayload or RatePayload or AggregatePercentilePayload or UpDownCounterPayload => true,
                _ => false
            };

        public static IDictionary<string, string> GetLabels(ICounterPayload payload)
        {
            string tags = payload.CombineTags();

            if (!IsMeterPayload(payload))
            {
                return CounterUtilities.GetMetadata(tags, ':');
            }

            // A later pair overwrites an earlier one with the same key, matching GetMetadata and
            // keeping Prometheus label names unique. CombineTags orders meter, instrument then value
            // tags, so the most specific tag wins.
            Dictionary<string, string> labels = new();
            foreach (KeyValuePair<string, string> tag in CounterTagFormatter.Decode(tags))
            {
                labels[tag.Key] = tag.Value;
            }

            return labels;
        }

        /// <summary>
        /// Renders an escaped tag string as the flat "key=value,key=value" form used by the metrics
        /// JSON output, with the keys and values unescaped.
        /// </summary>
        public static string Format(string tags)
        {
            // A counter without tags reports null, and callers serialize that as a JSON null. Decoding
            // would turn it into an empty string and change the shape of the output for every
            // untagged counter.
            if (string.IsNullOrEmpty(tags))
            {
                return tags;
            }

            StringBuilder builder = new();
            foreach (KeyValuePair<string, string> tag in CounterTagFormatter.Decode(tags))
            {
                if (builder.Length > 0)
                {
                    builder.Append(',');
                }

                builder.Append(tag.Key).Append('=').Append(tag.Value);
            }

            return builder.ToString();
        }
    }
}
