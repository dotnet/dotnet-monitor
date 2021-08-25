using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ReleaseTool.Core
{
    public class PassThroughLayoutWorker : ILayoutWorker
    {
        private readonly Func<FileInfo, bool> _shouldHandleFileFunc;
        private readonly Func<FileInfo, string> _getRelativePublishPathFromFileFunc;
        private readonly Func<FileInfo, FileMetadata> _getMetadataForFileFunc;
        private readonly string _stagingPath;

        public PassThroughLayoutWorker(
            Func<FileInfo, bool> shouldHandleFileFunc,
            Func<FileInfo, string> getRelativePublishPathFromFileFunc,
            Func<FileInfo, FileMetadata> getMetadataForFileFunc,
            string stagingPath)
        {

            _shouldHandleFileFunc = shouldHandleFileFunc ?? (_ => true);

            _getRelativePublishPathFromFileFunc = getRelativePublishPathFromFileFunc ?? (file => Path.Combine(FileMetadata.GetDefaultCatgoryForClass(FileClass.Unknown), file.Name));

            _getMetadataForFileFunc = getMetadataForFileFunc ?? ((FileInfo file) => GetDefaultFileMetadata(file, FileClass.Unknown));

            _stagingPath = stagingPath;
        }

        public void Dispose() {}

        public async ValueTask<LayoutWorkerResult> HandleFileAsync(FileInfo file, CancellationToken ct)
        {
            if (!_shouldHandleFileFunc(file))
            {
                return new LayoutWorkerResult(LayoutResultStatus.FileNotHandled);
            }

            string publishReleasePath = Path.Combine(_getRelativePublishPathFromFileFunc(file), file.Name);

            string localPath = file.FullName;

            if (_stagingPath is not null)
            {
                localPath = Path.Combine(_stagingPath, publishReleasePath);
                Directory.CreateDirectory(Path.GetDirectoryName(localPath));
                using (FileStream srcStream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read))
                using (FileStream destStream = new FileStream(localPath, FileMode.Create, FileAccess.Write))
                {
                    await srcStream.CopyToAsync(destStream, ct);
                }
            }

            var fileMap = new FileMapping(localPath, publishReleasePath);
            var metadata = _getMetadataForFileFunc(file);

            return new LayoutWorkerResult(
                    LayoutResultStatus.FileHandled,
                    new SingleFileResult(fileMap, metadata));
        }

        protected static FileMetadata GetDefaultFileMetadata(FileInfo fileInfo, FileClass fileClass)
        {
            string sha512Hash = GetSha512(fileInfo);
            FileMetadata result = new FileMetadata(
                fileClass,
                FileMetadata.GetDefaultCatgoryForClass(fileClass),
                sha512: sha512Hash);
            return result;
        }

        public static string GetSha512(FileInfo fileInfo)
        {
            using (FileStream fileReadStream = fileInfo.OpenRead())
            {
                byte[] hashValueBytes;
                using (System.Security.Cryptography.SHA512Managed sha = new System.Security.Cryptography.SHA512Managed())
                {
                    hashValueBytes = sha.ComputeHash(fileReadStream);
                }
                return BitConverter.ToString(hashValueBytes).Replace("-", String.Empty);
            }
        }
    }
}