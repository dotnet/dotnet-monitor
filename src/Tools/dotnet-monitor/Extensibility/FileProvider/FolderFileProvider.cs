// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Diagnostics.Tools.Monitor.Extensibility
{
    /// <summary>
    /// This class provides an implementation of <see cref="IFileProvider"/> with a root directory that does not need to exist at construction time.
    /// </summary>
    public class FolderFileProvider : IFileProvider
    {
        private IFileProvider _fileProvider = null;
        private readonly string _rootPath;

        private IFileProvider BaseProvider
        {
            get
            {
                if (_fileProvider == null)
                {
                    if (Directory.Exists(_rootPath))
                    {
                        _fileProvider = new PhysicalFileProvider(_rootPath);
                    }
                }
                return _fileProvider ?? new EmptyProvider();
            }
        }

        public FolderFileProvider(string rootPath)
        {
            _rootPath = rootPath;
        }

        /// <inheritdoc/>
        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            return BaseProvider.GetDirectoryContents(subpath);
        }

        /// <inheritdoc/>
        public IFileInfo GetFileInfo(string subpath)
        {
            return BaseProvider.GetFileInfo(subpath);
        }

        /// <inheritdoc/>
        public IChangeToken Watch(string filter)
        {
            return BaseProvider.Watch(filter);
        }

        private class EmptyProvider : IFileProvider
        {
            public IDirectoryContents GetDirectoryContents(string subpath)
            {
                return new MissingDirectoryContents();
            }

            public IFileInfo GetFileInfo(string subpath)
            {
                return new MissingFile();
            }

            public IChangeToken Watch(string filter)
            {
                throw new System.NotImplementedException();
            }
        }

        private class MissingDirectoryContents : IDirectoryContents
        {
            public bool Exists => false;

            public IEnumerator<IFileInfo> GetEnumerator()
            {
                yield break;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                yield break;
            }
        }

        private class MissingFile : IFileInfo
        {
            public bool Exists => false;

            public bool IsDirectory => throw new NotImplementedException();

            public DateTimeOffset LastModified => throw new NotImplementedException();

            public long Length => throw new NotImplementedException();

            public string Name => throw new NotImplementedException();

            public string PhysicalPath => throw new NotImplementedException();

            public Stream CreateReadStream()
            {
                throw new NotImplementedException();
            }
        }
    }
}
