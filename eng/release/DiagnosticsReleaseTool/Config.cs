// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace DiagnosticsReleaseTool.Impl
{
    internal class Config
    {
        public FileInfo ToolManifest { get; }
        public bool ShouldVerifyManifest { get; }
        public DirectoryInfo DropPath { get; }
        public DirectoryInfo StagingDirectory { get; }
        public string ReleaseName { get; }
        public string BuildVersion { get; }
        public string AccountName { get; }
        public string ClientId { get; }
        public string ContainerName { get; }

        public Config(
            FileInfo toolManifest,
            bool verifyToolManifest,
            DirectoryInfo inputDropPath,
            DirectoryInfo stagingDirectory,
            string releaseName,
            string buildVersion,
            string accountName,
            string clientId,
            string containerName)
        {
            ToolManifest = toolManifest;
            ShouldVerifyManifest = verifyToolManifest;
            DropPath = inputDropPath;
            StagingDirectory = stagingDirectory;
            ReleaseName = releaseName;
            BuildVersion = buildVersion;
            AccountName = accountName;
            ClientId = clientId;
            ContainerName = containerName;
        }
    }
}
