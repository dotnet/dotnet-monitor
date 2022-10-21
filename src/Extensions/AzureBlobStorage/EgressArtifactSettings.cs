﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Diagnostics.Monitoring.AzureStorage
{
    internal sealed class EgressArtifactSettings
    {
        /// <summary>
        /// The content encoding of the blob to be created.
        /// </summary>
        public string ContentEncoding { get; set; }

        /// <summary>
        /// The content type of the blob to be created.
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// The metadata of the blob to be created.
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; }
            = new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>
        /// Custom metadata of the blob to be created.
        /// </summary>
        public Dictionary<string, string> CustomMetadata { get; }
            = new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>
        /// Environment block of the target process.
        /// </summary>
        public Dictionary<string, string> EnvBlock { get; set; }
            = new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>
        /// The name of the artifact.
        /// </summary>
        public string Name { get; set; }
    }
}
