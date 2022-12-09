// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal static class DirectoryInfoExtensions
    {
        public static void CopyContentsTo(this DirectoryInfo srcDirInfo, DirectoryInfo targetDirInfo, bool overwrite)
        {
            foreach (DirectoryInfo subDirInfo in srcDirInfo.GetDirectories("*", SearchOption.TopDirectoryOnly))
            {
                // Skip symbolic links
                if (string.IsNullOrEmpty(subDirInfo.LinkTarget))
                {
                    CopyContentsTo(subDirInfo, targetDirInfo.CreateSubdirectory(subDirInfo.Name), overwrite);
                }
            }

            foreach (FileInfo fileInfo in srcDirInfo.GetFiles("*", SearchOption.TopDirectoryOnly))
            {
                // Skip symbolic links
                if (string.IsNullOrEmpty(fileInfo.LinkTarget))
                {
                    fileInfo.CopyTo(Path.Combine(targetDirInfo.FullName, fileInfo.Name), overwrite);
                }
            }
        }
    }
}
