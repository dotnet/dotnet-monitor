// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.LibrarySharing;
using Microsoft.Extensions.FileProviders;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public sealed class DefaultSharedLibraryInitializerTests : IDisposable
    {
        private const string ManagedTargetFramework = "custom";

        private readonly TestOutputLogger<DefaultSharedLibraryInitializer> _logger;
        private readonly TemporaryDirectory _sourceDir;
        private readonly TemporaryDirectory _targetDir;

        public DefaultSharedLibraryInitializerTests(ITestOutputHelper outputHelper)
        {
            _logger = new TestOutputLogger<DefaultSharedLibraryInitializer>(outputHelper);
            _sourceDir = new TemporaryDirectory(outputHelper);
            _targetDir = new TemporaryDirectory(outputHelper);
        }

        public void Dispose()
        {
            _targetDir.Dispose();
            _sourceDir.Dispose();
        }

        /// <summary>
        /// Validate that initialization fails if the source directory does not exist.
        /// </summary>
        [Fact]
        public async Task DefaultSharedLibraryInitializer_SourceNotExist_InitializeThrows()
        {
            DefaultSharedLibraryPathProvider provider = new(
                Path.Combine(_sourceDir.FullName, "doesNotExist"),
                _targetDir.FullName);

            using DefaultSharedLibraryInitializer initializer = new(provider, _logger);

            await Assert.ThrowsAsync<DirectoryNotFoundException>(() => initializer.InitializeAsync(CancellationToken.None));
        }

        /// <summary>
        /// Validate that initialization succeeds when there is not target directory.
        /// </summary>
        [Fact]
        public async Task DefaultSharedLibraryInitializer_SourceNoTarget_ReturnsSourcePath()
        {
            // Arrange
            string SourceFileName = "source.txt";
            string ExpectedFilePath = CreateSourceManagedFile(SourceFileName);

            DefaultSharedLibraryPathProvider provider = new(_sourceDir.FullName, null);

            // Act
            using DefaultSharedLibraryInitializer initializer = new(provider, _logger);

            IFileProviderFactory factory = await initializer.InitializeAsync(CancellationToken.None);

            // Assert
            Assert.NotNull(factory);

            IFileProvider managedProvider = factory.CreateManaged(ManagedTargetFramework);
            Assert.NotNull(managedProvider);

            IFileInfo managedDirInfo = managedProvider.GetFileInfo(SourceFileName);
            Assert.NotNull(managedDirInfo);

            Assert.Equal(ExpectedFilePath, managedDirInfo.PhysicalPath);
        }

        /// <summary>
        /// Validate that files copied to the target directory are readable.
        /// </summary>
        [Fact]
        public async Task DefaultSharedLibraryInitializer_SourceAndTarget_TargetReadAllowed()
        {
            // Arrange
            string SourceFileName = "source.txt";
            string SourceFilePath = CreateSourceManagedFile(SourceFileName);
            string TargetFilePath = CreateTargetManagedFilePath(SourceFileName);

            DefaultSharedLibraryPathProvider provider = new(_sourceDir.FullName, _targetDir.FullName);

            // Act
            using DefaultSharedLibraryInitializer initializer = new(provider, _logger);

            IFileProviderFactory factory = await initializer.InitializeAsync(CancellationToken.None);

            // Assert
            Assert.NotNull(factory);

            IFileProvider managedProvider = factory.CreateManaged(ManagedTargetFramework);
            Assert.NotNull(managedProvider);

            IFileInfo managedDirInfo = managedProvider.GetFileInfo(SourceFileName);
            Assert.NotNull(managedDirInfo);

            Assert.Equal(TargetFilePath, managedDirInfo.PhysicalPath);
            Assert.True(File.Exists(TargetFilePath));

            using StreamReader reader = CreateTargetFileReader(TargetFilePath);

            Assert.Equal(File.ReadAllText(SourceFilePath), reader.ReadToEnd());
        }


        /// <summary>
        /// Validate that files copied to the target directory cannot be edited or deleted
        /// during the user of the initializer.
        /// </summary>
        /// <remarks>
        /// File locks are advisory on non-Windows systems.
        /// </remarks>
        [ConditionalFact(typeof(TestConditions), nameof(TestConditions.IsWindows))]
        public async Task DefaultSharedLibraryInitializer_SourceAndTarget_TargetModifyDenied()
        {
            // Arrange
            string SourceFileName = "source.txt";
            CreateSourceManagedFile(SourceFileName);

            string TargetFilePath = CreateTargetManagedFilePath(SourceFileName);

            DefaultSharedLibraryPathProvider provider = new(_sourceDir.FullName, _targetDir.FullName);

            // Act
            using DefaultSharedLibraryInitializer initializer = new(provider, _logger);

            await initializer.InitializeAsync(CancellationToken.None);

            // Assert
            Assert.True(File.Exists(TargetFilePath));
            Assert.Throws<IOException>(() => CreateTargetFileWriter(TargetFilePath));
            Assert.Throws<IOException>(() => File.Delete(TargetFilePath));
            Assert.True(File.Exists(TargetFilePath));
        }

        /// <summary>
        /// Validate that files copied to the target directory can be edited after release of the initializer.
        /// </summary>
        [Fact]
        public async Task DefaultSharedLibraryInitializer_SourceAndTarget_TargetAllowWriteAfterRelease()
        {
            // Arrange
            string SourceFileName = "source.txt";
            CreateSourceManagedFile(SourceFileName);

            string TargetFilePath = CreateTargetManagedFilePath(SourceFileName);

            DefaultSharedLibraryPathProvider provider = new(_sourceDir.FullName, _targetDir.FullName);

            // Act
            using (DefaultSharedLibraryInitializer initializer = new(provider, _logger))
            {
                await initializer.InitializeAsync(CancellationToken.None);

                // Assert
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Assert.Throws<IOException>(() => CreateTargetFileWriter(TargetFilePath));
                }
            }

            CreateTargetFileWriter(TargetFilePath).Dispose();
        }

        /// <summary>
        /// Validate that files copied to the target directory can be deleted after release of the initializer.
        /// </summary>
        [Fact]
        public async Task DefaultSharedLibraryInitializer_SourceAndTarget_TargetAllowDeleteAfterRelease()
        {
            // Arrange
            string SourceFileName = "source.txt";
            CreateSourceManagedFile(SourceFileName);

            string TargetFilePath = CreateTargetManagedFilePath(SourceFileName);

            DefaultSharedLibraryPathProvider provider = new(_sourceDir.FullName, _targetDir.FullName);

            // Act
            using (DefaultSharedLibraryInitializer initializer = new(provider, _logger))
            {
                await initializer.InitializeAsync(CancellationToken.None);

                // Assert
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Assert.Throws<IOException>(() => File.Delete(TargetFilePath));
                }
            }

            File.Delete(TargetFilePath);
        }

        /// <summary>
        /// Validate that initialization will succeed if that target directory already has the files cached.
        /// </summary>
        [Fact]
        public async Task DefaultSharedLibraryInitializer_SourceAndTarget_TargetAlreadyCached()
        {
            // Arrange
            string SourceFileName = "source.txt";
            CreateSourceManagedFile(SourceFileName);

            string TargetFilePath = CreateTargetManagedFilePath(SourceFileName);

            DefaultSharedLibraryPathProvider provider = new(_sourceDir.FullName, _targetDir.FullName);

            // Act
            using (DefaultSharedLibraryInitializer initializer = new(provider, _logger))
            {
                await initializer.InitializeAsync(CancellationToken.None);
            }

            // Assert
            Assert.True(File.Exists(TargetFilePath));

            using (DefaultSharedLibraryInitializer initializer = new(provider, _logger))
            {
                await initializer.InitializeAsync(CancellationToken.None);
            }
        }

        /// <summary>
        /// Validate that initialization will succeed if size of file in target directory is a multiple
        /// of the default buffer size.
        /// </summary>
        [Fact]
        public async Task DefaultSharedLibraryInitializer_SourceAndTarget_TargetIsMultipleOfBufferSize()
        {
            // Arrange
            string SourceFileName = "source.txt";
            string SourceFilePath = CreateSourceManagedFile(SourceFileName);
            File.WriteAllText(SourceFilePath, new string('a', 2 * StreamDefaults.BufferSize));

            string TargetFilePath = CreateTargetManagedFilePath(SourceFileName);

            DefaultSharedLibraryPathProvider provider = new(_sourceDir.FullName, _targetDir.FullName);

            // Act
            using (DefaultSharedLibraryInitializer initializer = new(provider, _logger))
            {
                await initializer.InitializeAsync(CancellationToken.None);
            }

            // Assert
            Assert.True(File.Exists(TargetFilePath));

            using (DefaultSharedLibraryInitializer initializer = new(provider, _logger))
            {
                await initializer.InitializeAsync(CancellationToken.None);
            }
        }

        /// <summary>
        /// Validate that initialization will fail if the target directory already has the files cached
        /// but the files have been modified to be different.
        /// </summary>
        [Fact]
        public async Task DefaultSharedLibraryInitializer_SourceModifiedTarget_InitializeThrows()
        {
            // Arrange
            string SourceFileName = "source.txt";
            CreateSourceManagedFile(SourceFileName);

            string TargetFilePath = CreateTargetManagedFilePath(SourceFileName);

            DefaultSharedLibraryPathProvider provider = new(_sourceDir.FullName, _targetDir.FullName);

            // Act
            using (DefaultSharedLibraryInitializer initializer = new(provider, _logger))
            {
                await initializer.InitializeAsync(CancellationToken.None);
            }

            using (StreamWriter writer = CreateTargetFileWriter(TargetFilePath))
            {
                writer.Write("target.txt");
            }

            // Assert
            using (DefaultSharedLibraryInitializer initializer = new(provider, _logger))
            {
                await Assert.ThrowsAsync<InvalidOperationException>(() => initializer.InitializeAsync(CancellationToken.None));
            }
        }

        private string CreateSourceManagedFile(string fileName)
        {
            string path = Path.Combine(_sourceDir.FullName, "any", ManagedTargetFramework, fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, fileName);
            return path;
        }

        private string CreateTargetManagedFilePath(string fileName)
        {
            return Path.Combine(_targetDir.FullName, "any", ManagedTargetFramework, fileName);
        }

        private static StreamReader CreateTargetFileReader(string filePath)
        {
            return new StreamReader(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
        }

        private static StreamWriter CreateTargetFileWriter(string filePath)
        {
            return new StreamWriter(new FileStream(filePath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite));
        }
    }
}
