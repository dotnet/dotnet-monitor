// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.KeyPerFile;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal static class ConfigurationBuilderExtensions
    {
        // Works arounds an isssue in KeyPerFileConfigurationProvider where the change token is not signaled
        // when the keys and values are reloaded. See https://github.com/dotnet/aspnetcore/issues/32836
        public static IConfigurationBuilder AddKeyPerFileWithChangeTokenSupport(this IConfigurationBuilder builder, string directoryPath, bool optional, bool reloadOnChange)
        {
            return builder.Add(delegate (KeyPerFileConfigurationSourceWithChangeTokenSupport source)
            {
                if (!optional || Directory.Exists(directoryPath))
                {
                    source.FileProvider = new PhysicalFileProvider(directoryPath);
                }
                source.Optional = optional;
                source.ReloadOnChange = reloadOnChange;
            });
        }

        public static IConfigurationBuilder ConfigureStorageDefaults(this IConfigurationBuilder builder)
        {
            return builder.AddInMemoryCollection(new Dictionary<string, string>
            {
                {ConfigurationPath.Combine(ConfigurationKeys.Storage, nameof(StorageOptions.DumpTempFolder)), StorageOptionsDefaults.DumpTempFolder }
            });
        }

        private class KeyPerFileConfigurationSourceWithChangeTokenSupport :
            KeyPerFileConfigurationSource,
            IConfigurationSource
        {
            IConfigurationProvider IConfigurationSource.Build(IConfigurationBuilder builder)
            {
                return new KeyPerFileConfigurationProviderWithChangeTokenSupport(this);
            }
        }

        private class KeyPerFileConfigurationProviderWithChangeTokenSupport :
            KeyPerFileConfigurationProvider,
            IDisposable
        {
            // KeyPerFileConfigurationProvider.Load(bool) method
            private readonly static MethodInfo s_loadMethod = typeof(KeyPerFileConfigurationProvider).GetMethod(
                "Load",
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { typeof(bool) },
                null);

            private readonly IDisposable _changeTokenRegistration;

            public KeyPerFileConfigurationProviderWithChangeTokenSupport(KeyPerFileConfigurationSourceWithChangeTokenSupport source)
                : base(source)
            {
                Debug.Assert(null != s_loadMethod);
                if (null != s_loadMethod)
                {
                    if (source.ReloadOnChange && source.FileProvider != null)
                    {
                        _changeTokenRegistration = ChangeToken.OnChange(
                            () => source.FileProvider.Watch("*"),
                            () =>
                            {
                                Thread.Sleep(source.ReloadDelay);
                                Reload();
                            });
                    }
                }
            }

            void IDisposable.Dispose()
            {
                _changeTokenRegistration?.Dispose();
                base.Dispose();
            }

            private void Reload()
            {
                // Invoke KeyPerFileConfigurationProvider.Load(bool) to reload the keys and values.
                s_loadMethod.Invoke(this, new object[] { true });

                // The fix for the issue is to invoke the change token after reloading the data.
                OnReload();
            }
        }

    }
}
