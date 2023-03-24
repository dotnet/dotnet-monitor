// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Tools.Monitor.Extensibility;
using Microsoft.Diagnostics.Tools.Monitor.Extensibility.Egress;
using Microsoft.Extensions.Logging;
using System.IO;

namespace Microsoft.Diagnostics.Monitoring.Tool.TestHostingStartup
{
    internal sealed class BuildOutputExtensionRepository : ExtensionRepository
    {
        private readonly ILogger<EgressExtension> _logger;

        public BuildOutputExtensionRepository(ILogger<EgressExtension> logger)
        {
            _logger = logger;
        }

        public override bool TryFindExtension(string extensionName, out IExtension extension)
        {
            string libraryPath = GetExtensionPath(extensionName);
            if (Directory.Exists(libraryPath))
            {
                extension = CreateProgramExtension(extensionName, libraryPath);
                return true;
            }

            extension = null;
            return false;
        }

        private static string GetExtensionPath(string extensionName)
        {
            // The extension name happens to be the project name and output folder name
            // for the current list of extensions. This path looks like:
            // <repo>/artifacts/bin/<extension>/<configuration>/<tfm>
            return Path.Combine(
                BuildOutput.RootPath,
                extensionName,
                BuildOutput.ConfigurationName,
                TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker.ToFolderName());
        }

        private IExtension CreateProgramExtension(string extensionName, string extensionPath)
        {
            string manifestPath = Path.Combine(extensionPath, ExtensionManifest.DefaultFileName);

            return new EgressExtension(
                ExtensionManifest.FromPath(manifestPath),
                extensionPath,
                _logger);
        }
    }
}
