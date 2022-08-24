// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal static class DirectoryInfoExtensions
    {
        public static void CopyContentsTo(this DirectoryInfo srcDirInfo, DirectoryInfo targetDirInfo)
        {
            foreach (DirectoryInfo subDirInfo in srcDirInfo.GetDirectories())
            {
                CopyContentsTo(subDirInfo, targetDirInfo.CreateSubdirectory(subDirInfo.Name));
            }

            foreach (FileInfo fileInfo in srcDirInfo.GetFiles())
            {
                fileInfo.CopyTo(Path.Combine(targetDirInfo.FullName, fileInfo.Name));
            }
        }
    }
}
