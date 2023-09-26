// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    public static class EgressExtensibilityTestsConstants
    {
        public const string Key = "Key1";
        public const string Value = "Value1";
        public const string ExtensionsFolder = "extensions";
        public const string SampleArtifactPath = "sample\\path";
        public const string SampleFailureMessage = "the extension failed";
        public const string ProviderName = "TestingProvider"; // Must match the name in extension.json
        public const string AppName = "Microsoft.Diagnostics.Monitoring.EgressExtensibilityApp";
        public static readonly string DotnetToolsExtensionDir = Path.Combine(".store", "tool-name", "7.0", "tool-name", "7.0", "tools", "net7.0", "any");
        public const string DotnetToolsExeDir = "";
        public static readonly byte[] ByteArray = Encoding.ASCII.GetBytes(string.Empty);
    }
}
