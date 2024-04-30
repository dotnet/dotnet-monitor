// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.Egress.FileSystem;
using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.TestCommon.Options
{
    internal static partial class OptionsExtensions
    {
        public static RootOptions AddFileSystemEgress(this RootOptions options, string name, string outputPath)
        {
            var egressProvider = new FileSystemEgressProviderOptions()
            {
                DirectoryPath = outputPath
            };

            options.Egress = new EgressOptions
            {
                FileSystem = new Dictionary<string, FileSystemEgressProviderOptions>()
                {
                    { name, egressProvider }
                }
            };

            return options;
        }

        public static RootOptions AddGlobalCounter(this RootOptions options, int intervalSeconds)
        {
            options.GlobalCounter = new GlobalCounterOptions
            {
                IntervalSeconds = intervalSeconds
            };

            return options;
        }

        public static RootOptions AddProviderInterval(this RootOptions options, string name, int intervalSeconds)
        {
            Assert.NotNull(options.GlobalCounter);

            options.GlobalCounter.Providers.Add(name, new GlobalProviderOptions { IntervalSeconds = (float)intervalSeconds });

            return options;
        }

        public static CollectionRuleOptions CreateCollectionRule(this RootOptions rootOptions, string name)
        {
            CollectionRuleOptions options = new();
            rootOptions.CollectionRules.Add(name, options);
            return options;
        }

        public static RootOptions EnableInProcessFeatures(this RootOptions options)
        {
            if (null == options.InProcessFeatures)
            {
                options.InProcessFeatures = new Monitoring.Options.InProcessFeaturesOptions();
            }

            options.InProcessFeatures.Enabled = true;

            return options;
        }

        public static RootOptions SetConnectionMode(this RootOptions options, DiagnosticPortConnectionMode connectionMode)
        {
            if (null == options.DiagnosticPort)
            {
                options.DiagnosticPort = new DiagnosticPortOptions();
            }

            options.DiagnosticPort.ConnectionMode = connectionMode;

            return options;
        }

        public static RootOptions SetDefaultSharedPath(this RootOptions options, string directoryPath)
        {
            if (null == options.Storage)
            {
                options.Storage = new StorageOptions();
            }

            options.Storage.DefaultSharedPath = directoryPath;

            return options;
        }

        public static RootOptions SetDumpTempFolder(this RootOptions options, string directoryPath)
        {
            if (null == options.Storage)
            {
                options.Storage = new StorageOptions();
            }

            options.Storage.DumpTempFolder = directoryPath;

            return options;
        }

        /// <summary>
        /// Sets AzureAd authentication. Use this overload for most operations, unless specifically testing Authentication or Authorization.
        /// </summary>
        public static RootOptions UseAzureAd(this RootOptions options)
        {
            return options.UseAzureAd(
                tenantId: Guid.NewGuid().ToString("D"),
                clientId: Guid.NewGuid().ToString("D"),
                requiredRole: Guid.NewGuid().ToString("D"));
        }

        public static RootOptions UseAzureAd(this RootOptions options, string requiredRole)
        {
            return options.UseAzureAd(
                tenantId: Guid.NewGuid().ToString("D"),
                clientId: Guid.NewGuid().ToString("D"),
                requiredRole: requiredRole);
        }

        public static RootOptions UseAzureAd(this RootOptions options, string tenantId, string clientId, string requiredRole)
        {
            return options.UseAzureAd(new AzureAdOptions
            {
                TenantId = tenantId,
                ClientId = clientId,
                RequiredRole = requiredRole
            });
        }

        public static RootOptions UseAzureAd(this RootOptions options, AzureAdOptions azureAd)
        {
            if (null == options.Authentication)
            {
                options.Authentication = new AuthenticationOptions();
            }

            options.Authentication.AzureAd = azureAd;

            return options;
        }
    }
}
