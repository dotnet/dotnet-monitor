using System.IO;

namespace ReleaseTool.Core
{
    public sealed class BlobLayoutWorker : PassThroughLayoutWorker
    {
        public BlobLayoutWorker(string stagingPath) : base(
            shouldHandleFileFunc: static file => true,
            getRelativePublishPathFromFileFunc: static file => Helpers.GetDefaultPathForFileCategory(file, FileClass.Blob),
            getMetadataForFileFunc: static file => Helpers.GetDefaultFileMetadata(file, FileClass.Blob),
            stagingPath
        ){}
    }
}