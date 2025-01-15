// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    // Logger implementations have different ways of serializing log scopes. This class helps those loggers
    // serialize the scope information in the best way possible for each of the implementations.
    //
    // Handled examples:
    // - Simple Console Logger: only calls ToString, thus the data needs to be formatted in the ToString method.
    // - JSON Console Logger: checks for IReadOnlyCollection<KeyValuePair<string, object>> and formats each value
    //   in the enumeration; otherwise falls back to ToString.
    // - Event Log Logger: checks for IEnumerable<KeyValuePair<string, object>> and formats each value
    //   in the enumeration; otherwise falls back to ToString.
    // - Structured Logger: expects a IReadOnlyList<KeyValuePair<string, object>> and formats each value in the enumeration.
    public class KeyValueLogScope : IReadOnlyList<KeyValuePair<string, object>>
    {
        public List<KeyValuePair<string, object>> Values = new();

        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
        {
            return Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)Values).GetEnumerator();
        }

        int IReadOnlyCollection<KeyValuePair<string, object>>.Count => Values.Count;

        public KeyValuePair<string, object> this[int index] => Values[index];

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            foreach (var kvp in Values)
            {
                if (builder.Length > 0)
                {
                    builder.Append(' ');
                }
                builder.Append(kvp.Key);
                builder.Append(':');
                builder.Append(kvp.Value);
            }
            return builder.ToString();
        }
    }
}
