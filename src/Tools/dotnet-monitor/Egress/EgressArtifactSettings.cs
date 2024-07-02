// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress
{
    internal sealed class EgressArtifactSettings
    {
        /// <summary>
        /// The content type of the blob to be created.
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// The metadata of the blob to be created.
        /// </summary>
        public Dictionary<string, string> Metadata { get; }
            = new Dictionary<string, string>(StringComparer.Ordinal);

        public Dictionary<string, string> EnvBlock { get; set; }
            = new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>
        /// The name of the artifact.
        /// </summary>
        public string Name { get; set; }
    }
}
