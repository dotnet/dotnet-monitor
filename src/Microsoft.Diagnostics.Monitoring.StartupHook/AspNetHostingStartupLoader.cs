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
        private readonly string _filePath;
        private readonly string _simpleAssemblyName;

        private long _disposedState;

        public Assembly? HostingStartupAssembly { get; private set; }

        public AspNetHostingStartupLoader(string filePath)
        {
            _filePath = filePath;
            _simpleAssemblyName = CalculateSimpleAssemblyName(filePath);

            RegisterHostingStartupAssembly(_simpleAssemblyName);
            AssemblyLoadContext.Default.Resolving += AssemblyResolver;
        }

        private Assembly? AssemblyResolver(AssemblyLoadContext context, AssemblyName assemblyName)
        {
            if (HostingStartupAssembly != null)
            {
                return HostingStartupAssembly;
            }

            if (_simpleAssemblyName.Equals(assemblyName.Name, StringComparison.OrdinalIgnoreCase))
            {
                Assembly hostingStartupAssembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(_filePath);
                HostingStartupAssembly = hostingStartupAssembly;
            }

            return null;
        }

        private static string CalculateSimpleAssemblyName(string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            const string dllExtension = ".dll";
            if (dllExtension.Equals(Path.GetExtension(fileName), StringComparison.OrdinalIgnoreCase))
            {
                return fileName[..^dllExtension.Length];
            }

            return fileName;
        }

        private static void RegisterHostingStartupAssembly(string simpleAssemblyName)
        {
            const string HostingStartupEnvVariable = "ASPNETCORE_HOSTINGSTARTUPASSEMBLIES";
            // aspnetcore explicitly uses ; as the delimiter for the above environment variable.
            // ref: https://github.com/dotnet/aspnetcore/blob/898c164a1f537a8210a26eaf388bdc92531f6b09/src/Hosting/Hosting/src/Internal/WebHostOptions.cs#L79
            const char Delimiter = ';';

            string? curValue = Environment.GetEnvironmentVariable(HostingStartupEnvVariable);
            string newValue = string.IsNullOrWhiteSpace(curValue) ? simpleAssemblyName : string.Concat(curValue, Delimiter, simpleAssemblyName);
            Environment.SetEnvironmentVariable(HostingStartupEnvVariable, newValue);
        }

        public void Dispose()
        {
            if (!DisposableHelper.CanDispose(ref _disposedState))
                return;

            AssemblyLoadContext.Default.Resolving -= AssemblyResolver;
        }
    }
}
