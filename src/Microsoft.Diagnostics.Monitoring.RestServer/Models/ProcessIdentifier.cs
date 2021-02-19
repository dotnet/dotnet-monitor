﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json.Serialization;

#if UNITTEST
namespace Microsoft.Diagnostics.Monitoring.UnitTests.Models
#else
namespace Microsoft.Diagnostics.Monitoring.RestServer.Models
#endif
{
    public class ProcessIdentifier
    {
        [JsonPropertyName("pid")]
        public int Pid { get; set; }

        [JsonPropertyName("uid")]
        public Guid Uid { get; set; }
    }
}
