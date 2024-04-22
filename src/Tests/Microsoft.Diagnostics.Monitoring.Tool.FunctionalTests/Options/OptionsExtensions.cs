// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.Egress.FileSystem;
using System;
using System.Collections.Generic;

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

        public static CollectionRuleOptions CreateCollectionRule(this RootOptions rootOptions, string name)
        {
            CollectionRuleOptions options = new();
            rootOptions.CollectionRules.Add(name, options);
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
    }
}
