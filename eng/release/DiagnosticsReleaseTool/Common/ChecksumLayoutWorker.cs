using System.IO;

namespace ReleaseTool.Core
{
    public sealed class ChecksumLayoutWorker : PassThroughLayoutWorker
    {
        public ChecksumLayoutWorker(string stagingPath) : base(
            shouldHandleFileFunc: static file => file.Extension == ".sha512",
            getRelativePublishPathFromFileFunc: static file => Helpers.GetDefaultPathForFileCategory(file, FileClass.Checksum),
            getMetadataForFileFunc: static file => Helpers.GetDefaultFileMetadata(file, FileClass.Checksum),
            stagingPath
        ){}
    }
}