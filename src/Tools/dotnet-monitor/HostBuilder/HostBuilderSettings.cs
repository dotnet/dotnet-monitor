// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Tools.Monitor.Auth;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class HostBuilderSettings
    {
        private const string ProductFolderName = "dotnet-monitor";

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
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "." + ProductFolderName) :
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ProductFolderName));

        public string[]? Urls { get; set; }

        public string[]? MetricsUrls { get; set; }

        public bool EnableMetrics { get; set; }

        public string? DiagnosticPort { get; set; }

        public StartupAuthenticationMode AuthenticationMode { get; set; }

        public required string ContentRootDirectory { get; set; }

        public required string SharedConfigDirectory { get; set; }

        public required string UserConfigDirectory { get; set; }

        public FileInfo? UserProvidedConfigFilePath { get; set; }

        /// <summary>
        /// Create settings for dotnet-monitor hosting.
        /// </summary>
        public static HostBuilderSettings CreateMonitor(
            string[]? urls,
            string[]? metricUrls,
            bool metrics,
            string? diagnosticPort,
            StartupAuthenticationMode startupAuthMode,
            FileInfo? userProvidedConfigFilePath)
        {
            return new HostBuilderSettings()
            {
                Urls = urls,
                MetricsUrls = metricUrls,
                EnableMetrics = metrics,
                DiagnosticPort = diagnosticPort,
                AuthenticationMode = startupAuthMode,
                ContentRootDirectory = AppContext.BaseDirectory,
                SharedConfigDirectory = SharedConfigDirectoryPath,
                UserConfigDirectory = UserConfigDirectoryPath,
                UserProvidedConfigFilePath = userProvidedConfigFilePath
            };
        }

        private static string GetEnvironmentOverrideOrValue(string overrideEnvironmentVariable, string value)
        {
            return Environment.GetEnvironmentVariable(overrideEnvironmentVariable) ?? value;
        }
    }
}
