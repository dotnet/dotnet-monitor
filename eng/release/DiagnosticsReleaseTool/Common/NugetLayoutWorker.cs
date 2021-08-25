using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ReleaseTool.Core
{
    public sealed class NugetLayoutWorker : PassThroughLayoutWorker
    {
        public NugetLayoutWorker(string stagingPath) : base(
            shouldHandleFileFunc: ShouldHandleFile,
            getRelativePublishPathFromFileFunc: GetNugetPublishRelativePath,
            getMetadataForFileFunc: (FileInfo file) => GetDefaultFileMetadata(file, FileClass.Nuget),
            stagingPath
        ) {}

        private static bool ShouldHandleFile(FileInfo file) => file.Extension == ".nupkg" && !file.Name.EndsWith(".symbols.nupkg");
        private static string GetNugetPublishRelativePath(FileInfo file) => FileMetadata.GetDefaultCatgoryForClass(FileClass.Nuget);
    }
}