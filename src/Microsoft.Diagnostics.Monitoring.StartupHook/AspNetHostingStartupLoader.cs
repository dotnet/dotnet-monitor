// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

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
            ShortAssemblyName = CalculateAssemblyName(filePath);

            AppendToEnvironmentVariable(HostingStartupEnvVariable, ShortAssemblyName);
            AssemblyLoadContext.Default.Resolving += AssemblyResolver;
        }

        private Assembly? AssemblyResolver(AssemblyLoadContext context, AssemblyName assemblyName)
        {
            if (ShortAssemblyName.Equals(assemblyName.Name, StringComparison.OrdinalIgnoreCase))
            {
                return AssemblyLoadContext.Default.LoadFromAssemblyPath(FilePath);
            }

            return null;
        }

        private static string CalculateAssemblyName(string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            const string dllExtension = ".dll";
            if (dllExtension.Equals(Path.GetExtension(fileName), StringComparison.OrdinalIgnoreCase))
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

            AssemblyLoadContext.Default.Resolving -= AssemblyResolver;
        }
    }
}
