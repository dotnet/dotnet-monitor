// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using static System.Environment;

namespace Microsoft.Diagnostics.Tools.Monitor.Extensibility
{
    internal static class PreDefinedExtensionRepositories
    {

        public static IEnumerable<Func<IServiceProvider, IExtensionRepository>> GetExtensionRepositoryDelegates(IConfiguration configuration)
        {
            IConfigurationSection hostBuilderSettings = configuration.GetRequiredSection(ConfigurationKeys.InternalHostBuilderSettings);
            string progDataFolder = hostBuilderSettings.GetValue<string>(nameof(HostBuilderSettings.SharedConfigDirectory));
            string settingsFolder = hostBuilderSettings.GetValue<string>(nameof(HostBuilderSettings.UserConfigDirectory));

            if (string.IsNullOrWhiteSpace(progDataFolder))
            {
                throw new InvalidOperationException();
            }

            if (string.IsNullOrWhiteSpace(settingsFolder))
            {
                throw new InvalidOperationException();
            }

            FolderExtensionRepoDefinition[] folderProviders = new FolderExtensionRepoDefinition[]
            {
                // Folder next to dotnet-monitor assembly [Top Priority] 
                new FolderExtensionRepoDefinition(1000, Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)),

                // Folder in ProgramData or /etc/dotnet-monitor [Middle Priority] 
                new FolderExtensionRepoDefinition(2000, progDataFolder),
                
                // Folder next to user settings file [Lowest Priority] 
                new FolderExtensionRepoDefinition(3000, settingsFolder),
            };
            foreach (FolderExtensionRepoDefinition folderProvider in folderProviders)
            {
                yield return (IServiceProvider sp) => new FolderExtensionRepository(new PhysicalFileProvider(folderProvider.ExtensionDirectory), folderProvider.Priority, folderProvider.ExtensionDirectory);
            }
        }

        private class FolderExtensionRepoDefinition
        {
            private const string ExtensionFolder = "extensions";

            public readonly int Priority;
            public readonly string ExtensionDirectory;

            public FolderExtensionRepoDefinition(int priority, string rootPath)
            {
                Priority = priority;
                ExtensionDirectory = Path.Combine(rootPath, ExtensionFolder);
            }
        }
    }
}
