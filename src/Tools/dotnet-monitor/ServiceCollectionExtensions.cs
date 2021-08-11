// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Configuration;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers;
using Microsoft.Diagnostics.Tools.Monitor.Egress;
using Microsoft.Diagnostics.Tools.Monitor.Egress.AzureBlob;
using Microsoft.Diagnostics.Tools.Monitor.Egress.Configuration;
using Microsoft.Diagnostics.Tools.Monitor.Egress.FileSystem;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal static class ServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureMetrics(this IServiceCollection services, IConfiguration configuration)
        {
            return ConfigureOptions<MetricsOptions>(services, configuration, ConfigurationKeys.Metrics);
        }

        public static IServiceCollection ConfigureApiKeyConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            return ConfigureOptions<ApiAuthenticationOptions>(services, configuration, ConfigurationKeys.ApiAuthentication)
                // Loads and validates ApiAuthenticationOptions into ApiKeyAuthenticationOptions
                .AddSingleton<IPostConfigureOptions<ApiKeyAuthenticationOptions>, ApiKeyAuthenticationPostConfigureOptions>()
                // Notifies that ApiKeyAuthenticationOptions is changed when ApiAuthenticationOptions is changed.
                .AddSingleton<IOptionsChangeTokenSource<ApiKeyAuthenticationOptions>, ApiKeyAuthenticationOptionsChangeTokenSource>();
        }

        public static IServiceCollection ConfigureCollectionRules(this IServiceCollection services)
        {
            services.RegisterCollectionRuleAction<CollectDumpOptions>(KnownCollectionRuleActions.CollectDump);
            services.RegisterCollectionRuleAction<CollectGCDumpOptions>(KnownCollectionRuleActions.CollectGCDump);
            services.RegisterCollectionRuleAction<CollectLogsOptions>(KnownCollectionRuleActions.CollectLogs);
            services.RegisterCollectionRuleAction<CollectTraceOptions>(KnownCollectionRuleActions.CollectTrace);
            services.RegisterCollectionRuleAction<ExecuteOptions>(KnownCollectionRuleActions.Execute);

            services.RegisterCollectionRuleTrigger<AspNetRequestCountOptions>(KnownCollectionRuleTriggers.AspNetRequestCount);
            services.RegisterCollectionRuleTrigger<AspNetRequestDurationOptions>(KnownCollectionRuleTriggers.AspNetRequestDuration);
            services.RegisterCollectionRuleTrigger<AspNetResponseStatusOptions>(KnownCollectionRuleTriggers.AspNetResponseStatus);
            services.RegisterCollectionRuleTrigger<EventCounterOptions>(KnownCollectionRuleTriggers.EventCounter);
            services.RegisterCollectionRuleTrigger(KnownCollectionRuleTriggers.Startup);

            services.AddSingleton<ICollectionRuleActionOptionsProvider, CollectionRuleActionOptionsProvider>();
            services.AddSingleton<ICollectionRuleTriggerOptionsProvider, CollectionRuleTriggerOptionsProvider>();
            services.AddSingleton<IConfigureOptions<CollectionRuleOptions>, CollectionRuleConfigureNamedOptions>();
            services.AddSingleton<IValidateOptions<CollectionRuleOptions>, DataAnnotationValidateOptions<CollectionRuleOptions>>();

            return services;
        }

        public static IServiceCollection ConfigureActionValidation(this IServiceCollection services)
        {
            services.AddSingleton<IValidateOptions<ExecuteOptions>, ExecuteActionValidateOptions>();
            // Add other Action validation here as it is implemented.

            return services;
        }

        private static IServiceCollection RegisterCollectionRuleAction<TOptions>(this IServiceCollection services, string actionType) where TOptions : class, new()
        {
            services.TryAddSingletonEnumerable<ICollectionRuleActionProvider, CollectionRuleActionProvider<TOptions>>(sp => new CollectionRuleActionProvider<TOptions>(actionType));
            // NOTE: When opening colletion rule actions for extensibility, this should not be added for all registered actions.
            // Each action should register its own IValidateOptions<> implementation (if it needs one).
            services.AddSingleton<IValidateOptions<TOptions>, DataAnnotationValidateOptions<TOptions>>();
            return services;
        }

        private static IServiceCollection RegisterCollectionRuleTrigger(this IServiceCollection services, string triggerType)
        {
            services.TryAddSingletonEnumerable<ICollectionRuleTriggerProvider, CollectionRuleTriggerProvider>(sp => new CollectionRuleTriggerProvider(triggerType));
            return services;
        }

        private static IServiceCollection RegisterCollectionRuleTrigger<TOptions>(this IServiceCollection services, string triggerType) where TOptions : class, new()
        {
            services.TryAddSingletonEnumerable<ICollectionRuleTriggerProvider, CollectionRuleTriggerProvider<TOptions>>(sp => new CollectionRuleTriggerProvider<TOptions>(triggerType));
            // NOTE: When opening colletion rule triggers for extensibility, this should not be added for all registered triggers.
            // Each trigger should register its own IValidateOptions<> implementation (if it needs one).
            services.AddSingleton<IValidateOptions<TOptions>, DataAnnotationValidateOptions<TOptions>>();
            return services;
        }

        public static IServiceCollection ConfigureStorage(this IServiceCollection services, IConfiguration configuration)
        {
            return ConfigureOptions<StorageOptions>(services, configuration, ConfigurationKeys.Storage);
        }

        public static IServiceCollection ConfigureDefaultProcess(this IServiceCollection services, IConfiguration configuration)
        {
            return ConfigureOptions<ProcessFilterOptions>(services, configuration, ConfigurationKeys.DefaultProcess);
        }

        private static IServiceCollection ConfigureOptions<T>(IServiceCollection services, IConfiguration configuration, string key) where T : class
        {
            return services.Configure<T>(configuration.GetSection(key));
        }

        public static IServiceCollection ConfigureEgress(this IServiceCollection services)
        {
            // Register IEgressService implementation that provides egressing
            // of artifacts for the REST server.
            services.AddSingleton<IEgressService, EgressService>();

            services.AddSingleton<IEgressPropertiesConfigurationProvider, EgressPropertiesConfigurationProvider>();
            services.AddSingleton<IEgressPropertiesProvider, EgressPropertiesProvider>();

            // Register regress providers
            services.RegisterProvider<AzureBlobEgressProviderOptions, AzureBlobEgressProvider>(EgressProviderTypes.AzureBlobStorage);
            services.RegisterProvider<FileSystemEgressProviderOptions, FileSystemEgressProvider>(EgressProviderTypes.FileSystem);

            // Extra registrations for provider specific behavior
            services.AddSingleton<IPostConfigureOptions<AzureBlobEgressProviderOptions>, AzureBlobEgressPostConfigureOptions>();

            return services;
        }

        private static IServiceCollection RegisterProvider<TOptions, TProvider>(this IServiceCollection services, string name)
            where TProvider : class, IEgressProvider<TOptions>
            where TOptions : class
        {
            // Add services to provide raw configuration for the options type
            services.AddSingleton(sp => new EgressProviderConfigurationProvider<TOptions>(sp.GetRequiredService<IConfiguration>(), name));
            services.AddSingletonForwarder<IEgressProviderConfigurationProvider<TOptions>, EgressProviderConfigurationProvider<TOptions>>();
            services.TryAddSingletonEnumerableForwarder<IEgressProviderConfigurationProvider, EgressProviderConfigurationProvider<TOptions>>();

            // Add options services for configuring the options type
            services.AddSingleton<IConfigureOptions<TOptions>, EgressProviderConfigureNamedOptions<TOptions>>();
            services.AddSingleton<IValidateOptions<TOptions>, DataAnnotationValidateOptions<TOptions>>();

            // Register change sources for the options type
            services.AddSingleton<IOptionsChangeTokenSource<TOptions>, EgressPropertiesConfigurationChangeTokenSource<TOptions>>();
            services.AddSingleton<IOptionsChangeTokenSource<TOptions>, EgressProviderConfigurationChangeTokenSource<TOptions>>();

            // Add custom options cache to override behavior of default named options
            services.AddSingleton<IOptionsMonitorCache<TOptions>, EgressOptionsCache<TOptions>>();

            // Add egress provider and internal provider wrapper
            services.AddSingleton<IEgressProvider<TOptions>, TProvider>();
            services.AddSingleton<IEgressProviderInternal<TOptions>, EgressProviderInternal<TOptions>>();

            return services;
        }

        private static void AddSingletonForwarder<TService, TImplementation>(this IServiceCollection services) where TImplementation : class, TService where TService : class
        {
            services.AddSingleton<TService, TImplementation>(sp => sp.GetRequiredService<TImplementation>());
        }

        private static void TryAddSingletonEnumerableForwarder<TService, TImplementation>(this IServiceCollection services) where TImplementation : class, TService where TService : class
        {
            services.TryAddSingletonEnumerable<TService, TImplementation>(sp => sp.GetRequiredService<TImplementation>());
        }

        private static void TryAddSingletonEnumerable<TService, TImplementation>(this IServiceCollection services, Func<IServiceProvider, TImplementation> func) where TImplementation : class, TService where TService : class
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<TService, TImplementation>(func));
        }
    }
}
