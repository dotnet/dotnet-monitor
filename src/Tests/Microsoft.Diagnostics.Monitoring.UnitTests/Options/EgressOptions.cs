// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Monitoring.UnitTests.Options
{
    internal class EgressOptions
    {
        public Dictionary<string, Dictionary<string, string>> Providers { get; }
            = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, string> Properties { get; }
            = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }
}
