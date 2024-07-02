// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    /// <summary>
    /// Methods for loading test assemblies into the tool when running from the build output of the repository.
    /// </summary>
    internal static class TestAssemblies
    {
        private const string ArtifactsDirectoryName = "artifacts";
        private const string TestHostingStartupAssemblyName = "Microsoft.Diagnostics.Monitoring.Tool.TestHostingStartup";
        private const string TestStartupHookAssemblyName = "Microsoft.Diagnostics.Monitoring.Tool.TestStartupHook";

#nullable disable
        [Conditional("DEBUG")]
        public static void SimulateStartupHook()
        {
            // This code is to aid loading the TestStartupHook assembly when debug launching dotnet-monitor
            // so that additional manual configuration is not necessary. The functional tests already bootstrap
            // loading this assembly and initialize the hosting startup using standard dotnet environment variables.

            AssemblyLoadContext alc = AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly());
            // Skip attempting to load if it is already loaded
            if (!alc.Assemblies.Any(a => TestStartupHookAssemblyName.Equals(a.GetName().Name)))
            {
                // Only load if can compute the path correctly
                if (TryComputeBuildOutputAssemblyPath(TestStartupHookAssemblyName, out string startupHookAssemblyPath))
                {
                    // Load and simulate startup hook initialization
                    Assembly startupHookAssembly = alc.LoadFromAssemblyPath(startupHookAssemblyPath);
                    Type startupHookType = startupHookAssembly.GetType("StartupHook");
                    MethodInfo initializeMethod = startupHookType.GetMethod("Initialize", BindingFlags.Public | BindingFlags.Static);
                    initializeMethod.Invoke(null, Array.Empty<object>());
                }
            }
        }
#nullable restore

        [Conditional("DEBUG")]
        public static void AddHostingStartup(IWebHostBuilder builder)
        {
            // Only add the TestHostingStartup assembly if the assembly can be found in the build output
            if (TryComputeBuildOutputAssemblyPath(TestHostingStartupAssemblyName, out _))
            {
                // Get the existing list of hosting startup assemblies and append to it
                string hostingStartupAssemblies = builder.GetSetting(WebHostDefaults.HostingStartupAssembliesKey) ?? string.Empty;
                if (!hostingStartupAssemblies.Contains(TestHostingStartupAssemblyName, StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrEmpty(hostingStartupAssemblies))
                    {
                        hostingStartupAssemblies += ";";
                    }
                    hostingStartupAssemblies += TestHostingStartupAssemblyName;

                    builder.UseSetting(WebHostDefaults.HostingStartupAssembliesKey, hostingStartupAssemblies);
                }
            }
        }

        private static bool TryComputeBuildOutputAssemblyPath(string assemblyName, [NotNullWhen(true)] out string? path)
        {
            Assembly thisAssembly = Assembly.GetExecutingAssembly();
            string? thisAssemblyName = thisAssembly.GetName()?.Name;
            if (thisAssemblyName == null)
            {
                path = null;
                return false;
            }

            string separator = Path.DirectorySeparatorChar + ArtifactsDirectoryName + Path.DirectorySeparatorChar;
            int artifactsIndex = AppContext.BaseDirectory.IndexOf(separator);
            if (artifactsIndex != -1)
            {
                // This avoids accidentally renaming the "dotnet-monitor" folder that is the repository directory on disk.
                path = Path.Combine(
                    AppContext.BaseDirectory.Substring(0, artifactsIndex),
                    ArtifactsDirectoryName,
                    AppContext.BaseDirectory.Substring(artifactsIndex + separator.Length).Replace(thisAssemblyName, assemblyName),
                    $"{assemblyName}.dll");

                return File.Exists(path);
            }

            path = null;
            return false;
        }
    }
}
