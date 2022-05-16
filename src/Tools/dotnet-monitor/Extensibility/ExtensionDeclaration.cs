using System;

namespace Microsoft.Diagnostics.Tools.Monitor.Extensibility
{
    internal class ExtensionDeclaration
    {
        /// <summary>
        /// Id of the extension. This should be namespace qualified like nuget packages.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The version of the extension. This should be SemVer 2.0.
        /// </summary>
        public Version Version { get; set; }

        /// <summary>
        /// This is the command that will be executed in the 
        /// </summary>
        public string Program { get; set; }
    }
}
