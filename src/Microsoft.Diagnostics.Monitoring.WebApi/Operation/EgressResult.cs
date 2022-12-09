﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal struct EgressResult
    {
        public EgressResult(string value)
        {
            Value = value;
        }

        public string Value { get; }
    }
}
