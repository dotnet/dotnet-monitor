// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    /// <summary>
    /// Locates a self-contained, single-file build of the dotnet-monitor tool so that the functional
    /// tests can spawn it in place of the framework-dependent build.
    /// </summary>
    /// <remarks>
    /// The self-contained host is produced by the <c>DotNetMonitorBuildSelfContainedTool=true</c> build
    /// (see <c>src/Tools/dotnet-monitor/SelfContainedTool.targets</c>). The path is resolved from, in order:
    /// <list type="number">
    ///   <item>
    ///     The <c>DotNetMonitorTestSelfContainedToolPath</c> environment variable, which may point either at
    ///     the executable itself or at the directory that contains it. CI is expected to set this to the exact
    ///     published executable for the build under test.
    ///   </item>
    ///   <item>
    ///     A repo convention path of <c>&lt;artifacts&gt;/selfcontained-tool/&lt;rid&gt;/dotnet-monitor[.exe]</c>,
    ///     for local convenience after publishing the host there.
    ///   </item>
    /// </list>
    /// </remarks>
    public static class SelfContainedToolHelper
    {
        /// <summary>
        /// Environment variable that, when set, both selects the self-contained executable to run and switches
        /// every <c>MonitorRunner</c> to self-contained mode by default (so the whole functional suite runs
        /// against the self-contained host).
        /// </summary>
        public const string PathEnvironmentVariableName = "DotNetMonitorTestSelfContainedToolPath";

        private const string ToolName = "dotnet-monitor";

        private static readonly Lazy<string> s_pathLazy = new(ResolvePath);

        /// <summary>
        /// The resolved path to the self-contained dotnet-monitor executable. This is the path the tests will
        /// attempt to launch; it is not guaranteed to exist (see <see cref="IsAvailable"/>).
        /// </summary>
        public static string Path => s_pathLazy.Value;

        /// <summary>
        /// Gets whether <see cref="PathEnvironmentVariableName"/> was explicitly set. When set, the functional
        /// tests default to running every dotnet-monitor instance as self-contained.
        /// </summary>
        public static bool IsExplicitlyConfigured =>
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(PathEnvironmentVariableName));

        /// <summary>
        /// Gets whether a self-contained dotnet-monitor executable is present at <see cref="Path"/>. Used to
        /// gate self-contained-specific tests via <c>TestConditions</c>.
        /// </summary>
        public static bool IsAvailable => !string.IsNullOrEmpty(Path) && File.Exists(Path);

        /// <summary>
        /// Gets whether the functional tests should default to self-contained mode (i.e. the environment
        /// variable was explicitly set).
        /// </summary>
        public static bool IsEnabledByDefault => IsExplicitlyConfigured;

        private static string ResolvePath()
        {
            string fromEnvironment = Environment.GetEnvironmentVariable(PathEnvironmentVariableName);
            if (!string.IsNullOrEmpty(fromEnvironment))
            {
                // Allow pointing at either the executable or the directory that contains it.
                if (Directory.Exists(fromEnvironment))
                {
                    return System.IO.Path.Combine(fromEnvironment, ExecutableFileName);
                }

                return fromEnvironment;
            }

            return System.IO.Path.Combine(
                ArtifactsRootPath,
                "selfcontained-tool",
                RuntimeIdentifier,
                ExecutableFileName);
        }

        private static string ExecutableFileName =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ToolName + ".exe" : ToolName;

        // The executing assembly lives at <artifacts>/bin/<project>/<configuration>/<tfm>/<assembly>.dll;
        // walk up four directories to reach the artifacts root.
        private static string ArtifactsRootPath =>
            System.IO.Path.GetFullPath(
                System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    "..", "..", "..", ".."));

        private static string RuntimeIdentifier => string.Concat(OSPart, "-", ArchitecturePart);

        private static string OSPart
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return "win";
                }
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return "osx";
                }
                return "linux";
            }
        }

        private static string ArchitecturePart =>
            RuntimeInformation.OSArchitecture switch
            {
                Architecture.X64 => "x64",
                Architecture.X86 => "x86",
                Architecture.Arm64 => "arm64",
                Architecture.Arm => "arm",
                _ => RuntimeInformation.OSArchitecture.ToString("G").ToLowerInvariant()
            };
    }
}
