using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress
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
        public Dictionary<string, string> Metadata { get; }
            = new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>
        /// The name of the artifact.
        /// </summary>
        public string Name { get; set; }
    }
}
