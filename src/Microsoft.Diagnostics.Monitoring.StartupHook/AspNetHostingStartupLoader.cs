// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;

namespace Microsoft.Diagnostics.Monitoring.StartupHook
{
    internal sealed class AspNetHostingStartupLoader : IDisposable
    {
        private const string HostingStartupEnvVariable = "ASPNETCORE_HOSTINGSTARTUPASSEMBLIES";

        private readonly string FilePath;
        private readonly string ShortAssemblyName;

        private long _disposedState;

        public AspNetHostingStartupLoader(string filePath)
        {
            FilePath = filePath;
            ShortAssemblyName = CalculateShortAssemblyName(filePath);

            AppendToEnvironmentVariable(HostingStartupEnvVariable, filePath);
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolver!;
        }

        private Assembly? AssemblyResolver(object source, ResolveEventArgs e)
        {
            if (!e.Name.StartsWith(ShortAssemblyName, StringComparison.Ordinal))
            {
                return null;
            }

            return Assembly.LoadFile(FilePath);
        }

        private static string CalculateShortAssemblyName(string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            const string dllExtension = ".dll";
            if (fileName.EndsWith(dllExtension, StringComparison.OrdinalIgnoreCase))
            {
                return fileName[..^dllExtension.Length];
            }

            return fileName;
        }

        private static void AppendToEnvironmentVariable(string key, string value, string delimiter = ";")
        {
            string? curValue = Environment.GetEnvironmentVariable(key);
            string newValue = string.IsNullOrWhiteSpace(curValue) ? value : $"{curValue}{delimiter}{value}";
            Environment.SetEnvironmentVariable(key, newValue);
        }

        public void Dispose()
        {
            if (!DisposableHelper.CanDispose(ref _disposedState))
                return;

            AppDomain.CurrentDomain.AssemblyResolve -= AssemblyResolver!;
        }
    }
}
