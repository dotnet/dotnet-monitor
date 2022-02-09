using System;
using System.IO;

namespace ReleaseTool.Core
{
    public sealed class NugetLayoutWorker : PassThroughLayoutWorker
    {
        public NugetLayoutWorker(string stagingPath, Func<FileInfo, bool> additionalFileConstraint = null) : base(
            shouldHandleFileFunc: file => file.Extension == ".nupkg" && (null == additionalFileConstraint || additionalFileConstraint(file)),
            getRelativePublishPathFromFileFunc: static file => Helpers.GetDefaultPathForFileCategory(file, FileClass.Nuget),
            getMetadataForFileFunc: static file => Helpers.GetDefaultFileMetadata(file, FileClass.Nuget),
            stagingPath
        ){}
    }
}