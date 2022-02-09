using ReleaseTool.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ReleaseTool.Core
{
    internal class SkipPublisher : IPublisher
    {
        private readonly HashSet<string> _relativeOutputPaths = new(StringComparer.OrdinalIgnoreCase);

        public void Dispose()
        {
        }

        public Task<string> PublishFileAsync(FileMapping fileData, CancellationToken ct)
        {
            if (!_relativeOutputPaths.Add(fileData.RelativeOutputPath))
            {
                throw new InvalidOperationException($"File {fileData.LocalSourcePath} was already published to {fileData.RelativeOutputPath}.");
            }

            return Task.FromResult(fileData.RelativeOutputPath);
        }
    }
}
