// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Tools.Monitor.Extensibility
{
    internal class ToolsExtensionRepository : ExtensionRepository
    {
        private readonly string _targetFolder;
        private readonly IFileProvider _fileSystem;
        private readonly ILogger<ProgramExtension> _logger;

        private const string DotnetFolderName = "dotnet";
        private const string ToolsFolderName = "tools";

        // Location where extensions are stored by default.
        // Windows: "%USERPROFILE%\.dotnet\Tools"
        // Other: "%XDG_CONFIG_HOME%/.dotnet/tools" OR "%HOME%/.dotnet/tools" -> THIS HAS NOT BEEN TESTED YET ON LINUX
        public static readonly string DotnetToolsExtensionDirectoryPath =
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "." + DotnetFolderName, ToolsFolderName) :
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "." + DotnetFolderName, ToolsFolderName);

        public ToolsExtensionRepository(IFileProvider fileSystem, ILogger<ProgramExtension> logger, string targetFolder)
            : base(string.Format(CultureInfo.CurrentCulture, Strings.Message_FolderExtensionRepoName, targetFolder))
        {
            _fileSystem = fileSystem;

            _targetFolder = targetFolder;

            _logger = logger;
        }

        public override bool TryFindExtension(string extensionName, out IExtension extension)
        {
            const string storeDirectory = ".store";
            IDirectoryContents toolsStoreDir = _fileSystem.GetDirectoryContents(storeDirectory);

            foreach (IFileInfo tool in toolsStoreDir)
            {
                IFileInfo versionDirInfo = _fileSystem.GetDirectoryContents(Path.Combine(storeDirectory, tool.Name)).FirstOrDefault();
                if (null == versionDirInfo)
                {
                    continue;
                }
                string toolVersion = versionDirInfo.Name;

                string netVer = "net7.0"; // TODO: Still need to determine this

                string extensionPath = Path.Combine(storeDirectory, tool.Name, toolVersion, tool.Name, toolVersion, "tools", netVer, "any");

                if (_fileSystem.TryGetExtensionDefinitionPath(extensionPath, out string definitionPath))
                {
                    var currExtension = new ProgramExtension(extensionName, _targetFolder, _fileSystem, definitionPath, tool.Name, _logger);
                    if (extensionName == currExtension.Declaration.Name)
                    {
                        extension = currExtension;
                        return true;
                    }
                }
            }

            extension = null;
            return false;
        }
    }
}
