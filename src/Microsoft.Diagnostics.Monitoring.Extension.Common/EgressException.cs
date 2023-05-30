// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.Monitoring.Extension.Common
{
    /// <summary>
    /// Exception that egress providers can throw when an operational error occurs (e.g. failed to write the stream data).
    /// </summary>
    public sealed class EgressException : MonitoringException
    {
        public EgressException(string message) : base(message) { }

        public EgressException(string message, Exception innerException) : base(message, innerException) { }
    }
}
