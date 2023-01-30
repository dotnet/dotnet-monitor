// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Monitoring.S3
{
    /// <summary>
    /// Exception that egress providers can throw when an operational error occurs (e.g. failed to write the stream data).
    /// </summary>
    internal sealed class EgressException : MonitoringException
    {
        public EgressException(string message) : base(message) { }

        public EgressException(string message, Exception innerException) : base(message, innerException) { }
    }
}
