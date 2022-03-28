using System.Collections.Generic;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress
{
    public class ExtensionEgressPayload
    {
        public EgressArtifactSettings Settings { get; set; }
        public Dictionary<string, string> Properties { get; set; }
        public Dictionary<string, string> Configuration { get; set; }
        public string ProfileName { get; set; }
    }
}
