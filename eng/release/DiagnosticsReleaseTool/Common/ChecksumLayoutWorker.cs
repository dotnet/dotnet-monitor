// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ReleaseTool.Core
{
    public sealed class ChecksumLayoutWorker : PassThroughLayoutWorker
    {
        public ChecksumLayoutWorker(string stagingPath) : base(
            shouldHandleFileFunc: static file => file.Extension == ".sha512",
            getRelativePublishPathFromFileFunc: static file => Helpers.GetDefaultPathForFileCategory(file, FileClass.Checksum),
            getMetadataForFileFunc: static file => Helpers.GetDefaultFileMetadata(file, FileClass.Checksum),
            stagingPath
        )
        { }
    }
}
