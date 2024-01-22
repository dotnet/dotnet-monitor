// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace ReleaseTool.Core
{
    static class Helpers
    {
        internal static string GetDefaultPathForFileCategory(FileInfo file, FileClass fileClass)
        {
            string category = FileMetadata.GetDefaultCategoryForClass(fileClass);
            return FormattableString.Invariant($"{category}/{file.Name}");
        }

        internal static FileMetadata GetDefaultFileMetadata(FileInfo fileInfo, FileClass fileClass)
        {
            string sha512Hash = GetSha512(fileInfo);
            FileMetadata result = new FileMetadata(
                fileClass,
                FileMetadata.GetDefaultCategoryForClass(fileClass),
                sha512: sha512Hash);
            return result;
        }

        internal static string GetSha512(FileInfo fileInfo)
        {
            using FileStream fileReadStream = fileInfo.OpenRead();
            using var sha = System.Security.Cryptography.SHA512.Create();
            byte[] hashValueBytes = sha.ComputeHash(fileReadStream);
            return Convert.ToHexString(hashValueBytes);
        }
    }
}
