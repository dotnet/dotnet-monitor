// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor.Extensibility;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress
{
    internal class EgressArtifactResult : IExtensionResult
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
