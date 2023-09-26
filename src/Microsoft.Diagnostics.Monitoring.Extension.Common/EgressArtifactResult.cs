// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.Diagnostics.Monitoring.Extension.Common
{
    [DebuggerDisplay("{Succeeded?\"Succeeded\":\"Failed\",nq}: {Succeeded?ArtifactPath:FailureMessage}")]
    internal sealed class EgressArtifactResult
    {
        public bool Succeeded { get; set; }
        public string FailureMessage { get; set; }
        public string ArtifactPath { get; set; }

        public bool IsValid()
        {
            if (Succeeded)
            {
                // If Success, we must have an artifact path, and no failure message
                return !string.IsNullOrEmpty(ArtifactPath) && string.IsNullOrEmpty(FailureMessage);
            }
            else
            {
                // If Failure, we must have a failure message, and no artifact path
                return string.IsNullOrEmpty(ArtifactPath) && !string.IsNullOrEmpty(FailureMessage);
            }
        }
    }
}
