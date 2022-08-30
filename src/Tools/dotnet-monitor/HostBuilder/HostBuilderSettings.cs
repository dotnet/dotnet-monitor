// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class HostBuilderSettings
    {
        private const string ProductFolderName = "dotnet-monitor";
        private const string DotnetFolderName = "dotnet";
        private const string ToolsFolderName = "tools";

        // Allows tests to override the shared configuration directory so there
        // is better control and access of what is visible during test.
        private const string SharedConfigDirectoryOverrideEnvironmentVariable
            = "DotnetMonitorTestSettings__SharedConfigDirectoryOverride";

        // Allows tests to override the user configuration directory so there
        // is better control and access of what is visible during test.
        private const string UserConfigDirectoryOverrideEnvironmentVariable
            = "DotnetMonitorTestSettings__UserConfigDirectoryOverride";

        // Location where shared dotnet-monitor configuration is stored.
        // Windows: "%ProgramData%\dotnet-monitor
        // Other: /etc/dotnet-monitor
        private static readonly string SharedConfigDirectoryPath =
            GetEnvironmentOverrideOrValue(
                SharedConfigDirectoryOverrideEnvironmentVariable,
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), ProductFolderName) :
                    Path.Combine("/etc", ProductFolderName));

        // Location where user's dotnet-monitor configuration is stored.
        // Windows: "%USERPROFILE%\.dotnet-monitor"
        // Other: "%XDG_CONFIG_HOME%/dotnet-monitor" OR "%HOME%/.config/dotnet-monitor"
        private static readonly string UserConfigDirectoryPath =
            GetEnvironmentOverrideOrValue(
                UserConfigDirectoryOverrideEnvironmentVariable,
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "." + ProductFolderName, ToolsFolderName) :
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ProductFolderName, ToolsFolderName));

        // Location where extensions are stored by default.
        // Windows: "%USERPROFILE%\.dotnet"
        // Other: "%XDG_CONFIG_HOME%/dotnet" OR "%HOME%/.config/dotnet" -> IS THIS RIGHT?
        private static readonly string ExtensionDirectoryPath =
            GetEnvironmentOverrideOrValue(
                UserConfigDirectoryOverrideEnvironmentVariable,
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "." + DotnetFolderName) :
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), DotnetFolderName));

        public string[] Urls { get; set; }

        public string[] MetricsUrls { get; set; }

        public bool EnableMetrics { get; set; }

        public string DiagnosticPort { get; set; }

        public IAuthConfiguration Authentication { get; set; }

        public string ContentRootDirectory { get; set; }

        public string SharedConfigDirectory { get; set; }

        public string UserConfigDirectory { get; set; }

        public string ExtensionDirectory { get; set; }

        public FileInfo UserProvidedConfigFilePath { get; set; }

        /// <summary>
        /// Create settings for dotnet-monitor hosting.
        /// </summary>
        public static HostBuilderSettings CreateMonitor(
            string[] urls,
            string[] metricUrls,
            bool metrics,
            string diagnosticPort,
            IAuthConfiguration authConfiguration,
            FileInfo userProvidedConfigFilePath)
        {
            return new HostBuilderSettings()
            {
                Urls = urls,
                MetricsUrls = metricUrls,
                EnableMetrics = metrics,
                DiagnosticPort = diagnosticPort,
                Authentication = authConfiguration,
                ContentRootDirectory = AppContext.BaseDirectory,
                SharedConfigDirectory = SharedConfigDirectoryPath,
                UserConfigDirectory = UserConfigDirectoryPath,
                UserProvidedConfigFilePath = userProvidedConfigFilePath,
                ExtensionDirectory = ExtensionDirectoryPath
            };
        }

        private static string GetEnvironmentOverrideOrValue(string overrideEnvironmentVariable, string value)
        {
            return Environment.GetEnvironmentVariable(overrideEnvironmentVariable) ?? value;
        }
    }
}
