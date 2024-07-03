// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Options;
using System.IO;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    public class StoragePostConfigureOptions : IPostConfigureOptions<StorageOptions>
    {
        private const string DefaultSharedPathDumpsFolderName = "dumps";
        private const string DefaultSharedPathLibrariesFolderName = "libs";

        void IPostConfigureOptions<StorageOptions>.PostConfigure(string? name, StorageOptions options)
        {
            if (string.IsNullOrEmpty(options.DumpTempFolder))
            {
                if (!string.IsNullOrEmpty(options.DefaultSharedPath))
                {
                    options.DumpTempFolder = Path.Combine(options.DefaultSharedPath, DefaultSharedPathDumpsFolderName);
                }

                if (string.IsNullOrEmpty(options.DumpTempFolder))
                {
                    options.DumpTempFolder = Path.GetTempPath();
                }
            }

            if (string.IsNullOrEmpty(options.SharedLibraryPath))
            {
                if (!string.IsNullOrEmpty(options.DefaultSharedPath))
                {
                    options.SharedLibraryPath = Path.Combine(options.DefaultSharedPath, DefaultSharedPathLibrariesFolderName);
                }
            }
        }
    }
}
