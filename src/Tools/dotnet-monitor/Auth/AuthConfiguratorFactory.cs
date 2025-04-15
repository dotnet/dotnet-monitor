// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Validation;
using Microsoft.Diagnostics.Tools.Monitor.Auth.ApiKey;
using Microsoft.Diagnostics.Tools.Monitor.Auth.AzureAd;
using Microsoft.Diagnostics.Tools.Monitor.Auth.NoAuth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Tools.Monitor.Auth
{
    internal enum StartupAuthenticationMode
    {
        NoAuth,
        TemporaryKey,
        Deferred
    }

    internal static class AuthConfiguratorFactory
    {
        public static IAuthenticationConfigurator Create(StartupAuthenticationMode startupAuthMode, HostBuilderContext context, IServiceProvider services)
        {
            switch (startupAuthMode)
            {
                case StartupAuthenticationMode.NoAuth:
                    return new NoAuthConfigurator();

                case StartupAuthenticationMode.TemporaryKey:
                    if (context.Properties.TryGetValue(typeof(GeneratedJwtKey), out object? generatedJwtKeyObject))
                    {
                        if (generatedJwtKeyObject is GeneratedJwtKey generatedJwtKey)
                        {
                            return new MonitorApiKeyAuthConfigurator(generatedJwtKey);
                        }
                    }

                    // We should never reach here unless there is a bug in our initialization code.
                    throw new InvalidOperationException();

                case StartupAuthenticationMode.Deferred:
                    IConfigurationSection authConfigSection = context.Configuration.GetSection(ConfigurationKeys.Authentication);
                    AuthenticationOptions authOptions = new();
                    if (authConfigSection.Exists())
                    {
                        authConfigSection.Bind(authOptions);
                        ValidateAuthConfigSection(authOptions, authConfigSection.Path, services);
                    }

                    if (authOptions.AzureAd != null)
                    {
                        ValidateAuthConfigSection(authOptions.AzureAd, ConfigurationPath.Combine(authConfigSection.Path, ConfigurationKeys.AzureAd), services);
                        return new AzureAdAuthConfigurator(authOptions.AzureAd);
                    }

                    return new MonitorApiKeyAuthConfigurator();

                default:
                    throw new NotSupportedException();
            }
        }

        private static void ValidateAuthConfigSection<T>(T options, string configurationPath, IServiceProvider services) where T : notnull
        {
            var validationOptions = services.GetRequiredService<IOptions<ValidationOptions>>().Value;
            List<ValidationResult> results = new();
            if (!ValidationHelper.TryValidateObject(options, typeof(T), validationOptions, services, results))
            {
                throw new DeferredAuthenticationValidationException(configurationPath, results);
            }
        }
    }
}
