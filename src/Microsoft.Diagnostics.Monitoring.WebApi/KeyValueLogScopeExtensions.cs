// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal static class KeyValueLogScopeExtensions
    {
        public static void AddArtifactType(this KeyValueLogScope scope, string artifactType)
        {
            scope.Values.Add("ArtifactType", artifactType);
        }

        public static void AddArtifactProcessInfo(this KeyValueLogScope scope, IProcessInfo processInfo)
        {
            scope.Values.Add(
                ArtifactMetadataNames.ArtifactSource.ProcessId,
                processInfo.ProcessId.ToString(CultureInfo.InvariantCulture));
            scope.Values.Add(
                ArtifactMetadataNames.ArtifactSource.RuntimeInstanceCookie,
                processInfo.RuntimeInstanceCookie.ToString("N"));
        }
    }
}
