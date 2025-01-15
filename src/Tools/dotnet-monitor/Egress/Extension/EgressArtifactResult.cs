// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Microsoft.Diagnostics.Tools.Monitor.Extensibility;
using System.Diagnostics;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress
{
    [DebuggerDisplay("{Succeeded?\"Succeeded\":\"Failed\",nq}: {Succeeded?ArtifactPath:FailureMessage}")]
    internal sealed class EgressArtifactResult : IExtensionResult
    {
        public bool Succeeded { get; set; }
        public string FailureMessage { get; set; }
        public string ArtifactPath { get; set; }

        public bool IsValid()
        {
            if (Succeeded)
            {
                // If Success, we must have no failure message
                return string.IsNullOrEmpty(FailureMessage);
            }
            else
            {
                // If Failure, we must have a failure message, and no artifact path
                return string.IsNullOrEmpty(ArtifactPath) && !string.IsNullOrEmpty(FailureMessage);
            }
        }
    }
}
