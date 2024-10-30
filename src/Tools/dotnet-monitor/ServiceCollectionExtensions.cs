// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Diagnostics.Monitoring.EventPipe.Triggers;
using Microsoft.Diagnostics.Monitoring.EventPipe.Triggers.AspNet;
using Microsoft.Diagnostics.Monitoring.EventPipe.Triggers.EventCounter;
using Microsoft.Diagnostics.Monitoring.EventPipe.Triggers.SystemDiagnosticsMetrics;
using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Exceptions;
using Microsoft.Diagnostics.Tools.Monitor.Auth;
using Microsoft.Diagnostics.Tools.Monitor.Auth.ApiKey;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Configuration;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers.EventCounterShortcuts;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Triggers;
using Microsoft.Diagnostics.Tools.Monitor.Egress;
using Microsoft.Diagnostics.Tools.Monitor.Egress.Configuration;
using Microsoft.Diagnostics.Tools.Monitor.Egress.Extension;
using Microsoft.Diagnostics.Tools.Monitor.Egress.FileSystem;
using Microsoft.Diagnostics.Tools.Monitor.Exceptions;
using Microsoft.Diagnostics.Tools.Monitor.Extensibility;
using Microsoft.Diagnostics.Tools.Monitor.LibrarySharing;
using Microsoft.Diagnostics.Tools.Monitor.ParameterCapturing;
using Microsoft.Diagnostics.Tools.Monitor.Profiler;
using Microsoft.Diagnostics.Tools.Monitor.Stacks;
using Microsoft.Diagnostics.Tools.Monitor.StartupHook;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using Utils = Microsoft.Diagnostics.Monitoring.WebApi.Utilities;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal static class ServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureCors(this IServiceCollection services, IConfiguration configuration)
        {
            return ConfigureOptions<CorsConfigurationOptions>(services, configuration, ConfigurationKeys.CorsConfiguration);
        }

        public static IServiceCollection ConfigureDotnetMonitorDebug(this IServiceCollection services, IConfiguration configuration)
        {
            return ConfigureOptions<DotnetMonitorDebugOptions>(services, configuration, ConfigurationKeys.DotnetMonitorDebug);
        }

        public static IServiceCollection ConfigureGlobalCounter(this IServiceCollection services, IConfiguration configuration)
        {
            return ConfigureOptions<GlobalCounterOptions>(services, configuration, ConfigurationKeys.GlobalCounter)
                .AddSingleton<IValidateOptions<GlobalCounterOptions>, DataAnnotationValidateOptions<GlobalCounterOptions>>();

        }

        public static IServiceCollection ConfigureCollectionRuleDefaults(this IServiceCollection services, IConfiguration configuration)
        {
            return ConfigureOptions<CollectionRuleDefaultsOptions>(services, configuration, ConfigurationKeys.CollectionRuleDefaults);
        }

        public static IServiceCollection ConfigureTemplates(this IServiceCollection services, IConfiguration configuration)
        {
            return ConfigureOptions<TemplateOptions>(services, configuration, ConfigurationKeys.Templates);
        }

        public static IServiceCollection ConfigureInProcessFeatures(this IServiceCollection services, IConfiguration configuration)
        {
            ConfigureOptions<CallStacksOptions>(services, configuration, ConfigurationKeys.InProcessFeatures_CallStacks)
                .AddSingleton<IPostConfigureOptions<CallStacksOptions>, CallStacksPostConfigureOptions>();

            ConfigureOptions<ExceptionsOptions>(services, configuration, ConfigurationKeys.InProcessFeatures_Exceptions)
                .AddSingleton<IPostConfigureOptions<ExceptionsOptions>, ExceptionsPostConfigureOptions>();

            ConfigureOptions<ParameterCapturingOptions>(services, configuration, ConfigurationKeys.InProcessFeatures_ParameterCapturing)
             .AddSingleton<IPostConfigureOptions<ParameterCapturingOptions>, ParameterCapturingPostConfigureOptions>();

            ConfigureOptions<InProcessFeaturesOptions>(services, configuration, ConfigurationKeys.InProcessFeatures)
                .AddSingleton<InProcessFeaturesService>()
                .AddSingleton<IEndpointInfoSourceCallbacks, InProcessFeaturesEndpointInfoSourceCallbacks>();

            return services;
        }

        public static IServiceCollection ConfigureMetrics(this IServiceCollection services, IConfiguration configuration)
        {
            return ConfigureOptions<MetricsOptions>(services, configuration, ConfigurationKeys.Metrics)
                .AddSingleton<IValidateOptions<MetricsOptions>, DataAnnotationValidateOptions<MetricsOptions>>()
                .AddSingleton<MetricsStoreService>()
                .AddHostedService<MetricsService>()
                .AddSingleton<IMetricsPortsProvider, MetricsPortsProvider>();
        }

        public static IServiceCollection ConfigureMonitorApiKeyOptions(this IServiceCollection services, IConfiguration configuration, bool allowConfigurationUpdates)
        {
            ConfigureOptions<MonitorApiKeyOptions>(services, configuration, ConfigurationKeys.MonitorApiKey);

            // Loads and validates MonitorApiKeyOptions into MonitorApiKeyConfiguration
            services.AddSingleton<IPostConfigureOptions<MonitorApiKeyConfiguration>, MonitorApiKeyPostConfigure>();

            if (allowConfigurationUpdates)
            {
                // Notifies that MonitorApiKeyConfiguration is changed when MonitorApiKeyOptions is changed.
                services.AddSingleton<IOptionsChangeTokenSource<MonitorApiKeyConfiguration>, MonitorApiKeyChangeTokenSource>();
            }

            return services;
        }

        public static AuthenticationBuilder ConfigureMonitorApiKeyAuthentication(this IServiceCollection services, IConfiguration configuration, AuthenticationBuilder builder, bool allowConfigurationUpdates)
        {
            IConfigurationSection authSection = configuration.GetSection(ConfigurationKeys.Authentication);
            services.ConfigureMonitorApiKeyOptions(authSection, allowConfigurationUpdates);

            // Notifies that the JwtBearerOptions change when MonitorApiKeyConfiguration gets changed.
            services.AddSingleton<IOptionsChangeTokenSource<JwtBearerOptions>, JwtBearerChangeTokenSource>();
            // Adds the JwtBearerOptions configuration source, which will provide the updated JwtBearerOptions when MonitorApiKeyConfiguration updates
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<JwtBearerOptions>, JwtBearerPostConfigure>());

            // AddJwtBearer will consume the JwtBearerOptions generated by ConfigureMonitorApiKeyConfiguration
            builder.AddScheme<JwtBearerOptions, JwtBearerHandler>(JwtBearerDefaults.AuthenticationScheme, null, _ => { });

            if (allowConfigurationUpdates)
            {
                services.AddSingleton<MonitorApiKeyConfigurationObserver>();
            }

            return builder;
        }

        public static IServiceCollection ConfigureCollectionRules(this IServiceCollection services)
        {
            services.RegisterCollectionRuleAction<CollectDumpActionFactory, CollectDumpOptions>(KnownCollectionRuleActions.CollectDump);
            services.RegisterCollectionRuleAction<CollectExceptionsActionFactory, CollectExceptionsOptions>(KnownCollectionRuleActions.CollectExceptions);
            services.RegisterCollectionRuleAction<CollectGCDumpActionFactory, CollectGCDumpOptions>(KnownCollectionRuleActions.CollectGCDump);
            services.RegisterCollectionRuleAction<CollectLiveMetricsActionFactory, CollectLiveMetricsOptions>(KnownCollectionRuleActions.CollectLiveMetrics);
            services.RegisterCollectionRuleAction<CollectLogsActionFactory, CollectLogsOptions>(KnownCollectionRuleActions.CollectLogs);
            services.RegisterCollectionRuleAction<CollectStacksActionFactory, CollectStacksOptions>(KnownCollectionRuleActions.CollectStacks);
            services.RegisterCollectionRuleAction<CollectTraceActionFactory, CollectTraceOptions>(KnownCollectionRuleActions.CollectTrace);
            services.RegisterCollectionRuleAction<ExecuteActionFactory, ExecuteOptions>(KnownCollectionRuleActions.Execute);
            services.RegisterCollectionRuleAction<LoadProfilerActionFactory, LoadProfilerOptions>(KnownCollectionRuleActions.LoadProfiler);
            services.RegisterCollectionRuleAction<SetEnvironmentVariableActionFactory, SetEnvironmentVariableOptions>(KnownCollectionRuleActions.SetEnvironmentVariable);
            services.RegisterCollectionRuleAction<GetEnvironmentVariableActionFactory, GetEnvironmentVariableOptions>(KnownCollectionRuleActions.GetEnvironmentVariable);

            services.RegisterCollectionRuleTrigger<CollectionRules.Triggers.AspNetRequestCountTriggerFactory, AspNetRequestCountOptions>(KnownCollectionRuleTriggers.AspNetRequestCount);
            services.RegisterCollectionRuleTrigger<CollectionRules.Triggers.AspNetRequestDurationTriggerFactory, AspNetRequestDurationOptions>(KnownCollectionRuleTriggers.AspNetRequestDuration);
            services.RegisterCollectionRuleTrigger<CollectionRules.Triggers.AspNetResponseStatusTriggerFactory, AspNetResponseStatusOptions>(KnownCollectionRuleTriggers.AspNetResponseStatus);
            services.RegisterCollectionRuleTrigger<CollectionRules.Triggers.EventCounterTriggerFactory, EventCounterOptions>(KnownCollectionRuleTriggers.EventCounter);
            services.RegisterCollectionRuleTrigger<CollectionRules.Triggers.EventCounterTriggerFactory, CPUUsageOptions>(KnownCollectionRuleTriggers.CPUUsage);
            services.RegisterCollectionRuleTrigger<CollectionRules.Triggers.EventCounterTriggerFactory, GCHeapSizeOptions>(KnownCollectionRuleTriggers.GCHeapSize);
            services.RegisterCollectionRuleTrigger<CollectionRules.Triggers.EventCounterTriggerFactory, ThreadpoolQueueLengthOptions>(KnownCollectionRuleTriggers.ThreadpoolQueueLength);
            services.RegisterCollectionRuleTrigger<StartupTriggerFactory>(KnownCollectionRuleTriggers.Startup);
            services.RegisterCollectionRuleTrigger<CollectionRules.Triggers.EventMeterTriggerFactory, EventMeterOptions>(KnownCollectionRuleTriggers.EventMeter);

            services.AddSingleton<EventPipeTriggerFactory>();
            services.AddSingleton<ITraceEventTriggerFactory<EventCounterTriggerSettings>, Monitoring.EventPipe.Triggers.EventCounter.EventCounterTriggerFactory>();
            services.AddSingleton<ITraceEventTriggerFactory<AspNetRequestDurationTriggerSettings>, Monitoring.EventPipe.Triggers.AspNet.AspNetRequestDurationTriggerFactory>();
            services.AddSingleton<ITraceEventTriggerFactory<AspNetRequestCountTriggerSettings>, Monitoring.EventPipe.Triggers.AspNet.AspNetRequestCountTriggerFactory>();
            services.AddSingleton<ITraceEventTriggerFactory<AspNetRequestStatusTriggerSettings>, Monitoring.EventPipe.Triggers.AspNet.AspNetRequestStatusTriggerFactory>();
            services.AddSingleton<ITraceEventTriggerFactory<SystemDiagnosticsMetricsTriggerSettings>, Monitoring.EventPipe.Triggers.SystemDiagnosticsMetrics.SystemDiagnosticsMetricsTriggerFactory>();

            services.AddSingleton<CollectionRulesConfigurationProvider>();
            services.AddSingleton<ICollectionRuleActionOperations, CollectionRuleActionOperations>();
            services.AddSingleton<ICollectionRuleTriggerOperations, CollectionRuleTriggerOperations>();

            services.AddSingleton<IPostConfigureOptions<TemplateOptions>, TemplatesPostConfigureOptions>();

            services.AddSingleton<IConfigureOptions<CollectionRuleOptions>, CollectionRuleConfigureNamedOptions>();
            services.AddSingleton<IPostConfigureOptions<CollectionRuleOptions>, CollectionRulePostConfigureOptions>();
            services.AddSingleton<IPostConfigureOptions<CollectionRuleOptions>, DefaultCollectionRulePostConfigureOptions>();
            services.AddSingleton<IValidateOptions<CollectionRuleOptions>, DataAnnotationValidateOptions<CollectionRuleOptions>>();

            // Register change sources for the options type
            services.AddSingleton<IOptionsChangeTokenSource<CollectionRuleOptions>, CollectionRulesConfigurationChangeTokenSource>();

            // Add custom options cache to override behavior of default named options
            services.AddSingleton<IOptionsMonitorCache<CollectionRuleOptions>, DynamicNamedOptionsCache<CollectionRuleOptions>>();

            services.AddSingleton<ActionListExecutor>();
            services.AddSingletonForwarder<ICollectionRuleService, CollectionRuleService>();
            services.AddSingleton<CollectionRuleService>();
            services.AddHostedServiceForwarder<CollectionRuleService>();

            services.AddSingleton<IEndpointInfoSourceCallbacks, CollectionRuleEndpointInfoSourceCallbacks>();

            return services;
        }

        public static IServiceCollection RegisterCollectionRuleAction<TFactory, TOptions>(this IServiceCollection services, string actionName)
            where TFactory : class, ICollectionRuleActionFactory<TOptions>
            where TOptions : BaseRecordOptions, new()
        {
            services.AddSingleton<TFactory>();
            services.AddSingleton<CollectionRuleActionFactoryProxy<TFactory, TOptions>>();
            services.AddSingleton<ICollectionRuleActionDescriptor, CollectionRuleActionDescriptor<TFactory, TOptions>>(sp => new CollectionRuleActionDescriptor<TFactory, TOptions>(actionName));
            // NOTE: When opening collection rule actions for extensibility, this should not be added for all registered actions.
            // Each action should register its own IValidateOptions<> implementation (if it needs one).
            services.AddSingleton<IValidateOptions<TOptions>, DataAnnotationValidateOptions<TOptions>>();
            return services;
        }

        public static IServiceCollection RegisterCollectionRuleTrigger<TFactory>(this IServiceCollection services, string triggerName)
            where TFactory : class, ICollectionRuleTriggerFactory
        {
            services.AddSingleton<TFactory>();
            services.AddSingleton<CollectionRuleTriggerFactoryProxy<TFactory>>();
            services.AddSingleton<ICollectionRuleTriggerDescriptor, CollectionRuleTriggerDescriptor<TFactory>>(
                sp => new CollectionRuleTriggerDescriptor<TFactory>(triggerName));
            return services;
        }

        public static IServiceCollection RegisterCollectionRuleTrigger<TFactory, TOptions>(this IServiceCollection services, string triggerName)
            where TFactory : class, ICollectionRuleTriggerFactory<TOptions>
            where TOptions : class, new()
        {
            services.AddSingleton<TFactory>();
            services.AddSingleton<CollectionRuleTriggerFactoryProxy<TFactory, TOptions>>();
            services.AddSingleton<ICollectionRuleTriggerDescriptor, CollectionRuleTriggerProvider<TFactory, TOptions>>(
                sp => new CollectionRuleTriggerProvider<TFactory, TOptions>(triggerName));
            // NOTE: When opening collection rule triggers for extensibility, this should not be added for all registered triggers.
            // Each trigger should register its own IValidateOptions<> implementation (if it needs one).
            services.AddSingleton<IValidateOptions<TOptions>, DataAnnotationValidateOptions<TOptions>>();

            return services;
        }

        public static IServiceCollection ConfigureStorage(this IServiceCollection services, IConfiguration configuration)
        {
            ConfigureOptions<StorageOptions>(services, configuration, ConfigurationKeys.Storage);
            services.AddSingleton<IPostConfigureOptions<StorageOptions>, StoragePostConfigureOptions>();
            return services;
        }

        public static IServiceCollection ConfigureDefaultProcess(this IServiceCollection services, IConfiguration configuration)
        {
            return ConfigureOptions<ProcessFilterOptions>(services, configuration, ConfigurationKeys.DefaultProcess);
        }

        private static IServiceCollection ConfigureOptions<T>(IServiceCollection services, IConfiguration configuration, string key) where T : class
        {
            return services.Configure<T>(configuration.GetSection(key));
        }

        public static IServiceCollection ConfigureExtensions(this IServiceCollection services)
        {
            // Extension discovery
            services.AddSingleton<ExtensionDiscoverer>();
            // Extension type factories
            services.AddSingleton<EgressExtensionFactory>();
            // Well-known extensions
            services.AddSingleton<ExtensionRepository, WellKnownExtensionRepository>();
            services.AddSingleton<IWellKnownExtensionFactory, FileSystemEgressExtensionFactory>();
            return services;
        }

        public static IServiceCollection ConfigureExtensionLocations(this IServiceCollection services, HostBuilderSettings settings)
        {
            services.TryAddSingleton<IDotnetToolsFileSystem, DefaultDotnetToolsFileSystem>();

            string progDataFolder = settings.SharedConfigDirectory;
            string settingsFolder = settings.UserConfigDirectory;

            if (string.IsNullOrWhiteSpace(progDataFolder)
                || string.IsNullOrWhiteSpace(settingsFolder))
            {
                throw new InvalidOperationException();
            }

            // Add the folders we search to get extensions from
            services.AddFolderExtensionRepository(AppContext.BaseDirectory);
            services.AddFolderExtensionRepository(progDataFolder);
            services.AddFolderExtensionRepository(settingsFolder);

            return services;
        }

        public static IServiceCollection AddFolderExtensionRepository(this IServiceCollection services, string path)
        {
            const string ExtensionFolder = "extensions";

            string targetExtensionFolder = Path.Combine(path, ExtensionFolder);

            Func<IServiceProvider, ExtensionRepository> createDelegate =
                (IServiceProvider serviceProvider) =>
                {
                    IFileProvider fileProvider = GetFileProvider(targetExtensionFolder);
                    EgressExtensionFactory egressExtensionFactory = serviceProvider.GetRequiredService<EgressExtensionFactory>();
                    ILogger<FolderExtensionRepository> logger = serviceProvider.GetRequiredService<ILogger<FolderExtensionRepository>>();
                    return new FolderExtensionRepository(fileProvider, egressExtensionFactory, logger);
                };

            services.AddSingleton<ExtensionRepository>(createDelegate);

            return services;
        }

        private static IFileProvider GetFileProvider(string targetExtensionFolder)
        {
            if (Directory.Exists(targetExtensionFolder))
            {
                return new PhysicalFileProvider(targetExtensionFolder);
            }

            return new NullFileProvider();
        }

        public static IServiceCollection ConfigureEgress(this IServiceCollection services)
        {
            services.AddSingleton<IEgressConfigurationProvider, EgressConfigurationProvider>();
            services.AddSingleton<EgressProviderSource>();
            services.AddSingleton<IEgressService, EgressService>();
            services.AddHostedService<EgressValidationService>();

            return services;
        }

        public static IServiceCollection ConfigureDiagnosticPort(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<DiagnosticPortOptions>(configuration.GetSection(ConfigurationKeys.DiagnosticPort));
            services.AddSingleton<IPostConfigureOptions<DiagnosticPortOptions>, DiagnosticPortPostConfigureOptions>();
            services.AddSingleton<IValidateOptions<DiagnosticPortOptions>, DiagnosticPortValidateOptions>();

            return services;
        }

        public static IServiceCollection ConfigureLibrarySharing(this IServiceCollection services)
        {
            services.AddSingleton<SharedLibraryService>();
            services.AddSingletonForwarder<ISharedLibraryService, SharedLibraryService>();
            services.AddHostedServiceForwarder<SharedLibraryService>();
            services.TryAddSingleton<ISharedLibraryInitializer, DefaultSharedLibraryInitializer>();
            services.AddSingleton<DefaultSharedLibraryPathProvider>();
            return services;
        }

        public static IServiceCollection ConfigureProfiler(this IServiceCollection services)
        {
            services.AddSingleton<ProfilerService>();
            services.AddSingleton<IEndpointInfoSourceCallbacks, ProfilerEndpointInfoSourceCallbacks>();
            return services;
        }

        public static IServiceCollection ConfigureExceptions(this IServiceCollection services)
        {
            services.AddTransient<IExceptionsOperationFactory, ExceptionsOperationFactory>();
            services.AddScoped<IExceptionsStore, ExceptionsStore>();
            services.AddScoped<IExceptionsStoreCallbackFactory, ExceptionsStoreLimitsCallbackFactory>();
            services.AddScoped<IDiagnosticLifetimeService, ExceptionsService>();
            return services;
        }

        public static IServiceCollection ConfigureRequestLimits(this IServiceCollection services)
        {
            services.AddSingleton<IRequestLimitTracker, RequestLimitTracker>();
            services.AddSingleton((_) => { return new RequestLimit(Utils.ArtifactType_Dump, 1); });
            services.AddSingleton((_) => { return new RequestLimit(Utils.ArtifactType_GCDump, 1); });
            services.AddSingleton((_) => { return new RequestLimit(Utils.ArtifactType_Logs, 3); });
            services.AddSingleton((_) => { return new RequestLimit(Utils.ArtifactType_Trace, 3); });
            services.AddSingleton((_) => { return new RequestLimit(Utils.ArtifactType_Metrics, 3); });
            services.AddSingleton((_) => { return new RequestLimit(Utils.ArtifactType_Stacks, 1); });
            services.AddSingleton((_) => { return new RequestLimit(Utils.ArtifactType_Exceptions, 1); });
            services.AddSingleton((_) => { return new RequestLimit(Utils.ArtifactType_Parameters, 1); });
            services.AddSingleton((_) => { return new RequestLimit(RequestLimitTracker.Unlimited, int.MaxValue); });

            return services;
        }

        public static IServiceCollection ConfigureStartupHook(this IServiceCollection services)
        {
            services.AddScoped<StartupHookApplicator>();
            services.AddScoped<StartupHookService>();
            services.AddScopedForwarder<IDiagnosticLifetimeService, StartupHookService>();
            return services;
        }

        public static IServiceCollection ConfigureStartupLoggers(this IServiceCollection services, IAuthenticationConfigurator authConfigurator)
        {
            services.AddSingleton<IStartupLogger, ExperienceSurveyStartupLogger>();
            services.AddSingleton<IStartupLogger, ExperimentalStartupLogger>();
            services.AddSingleton<IStartupLogger, HostBuilderStartupLogger>();
            services.AddSingleton<IStartupLogger, DiagnosticPortStartupLogger>();
            services.AddSingleton<IStartupLogger, ElevatedPermissionsStartupLogger>();
            services.AddSingleton<IStartupLogger, AddressListenResultsStartupLogger>();
            services.AddSingleton<IStartupLogger>((services) =>
            {
                ILogger<Startup> logger = services.GetRequiredService<ILogger<Startup>>();
                return authConfigurator.CreateStartupLogger(logger, services);
            });
            services.AddSingleton<IStartupLogger, EgressStartupLogger>();
            return services;
        }

        public static IServiceCollection ConfigureParameterCapturing(this IServiceCollection services)
        {
            services.AddTransient<ICaptureParametersOperationFactory, CaptureParametersOperationFactory>();
            return services;
        }
        public static IServiceCollection ConfigureEndpointInfoSource(this IServiceCollection services)
        {
            services.AddSingleton<IEndpointInfoSource, FilteredEndpointInfoSource>();

            services.AddSingleton<IServerEndpointStateChecker, ServerEndpointStateChecker>();

            if (ToolIdentifiers.IsEnvVarEnabled(ExperimentalFeatureIdentifiers.EnvironmentVariables.ServerEndpointPruningAlgorithmV2))
            {
                services.AddSingleton<ServerEndpointTrackerV2>();
                services.AddSingletonForwarder<IServerEndpointTracker, ServerEndpointTrackerV2>();
                services.AddHostedServiceForwarder<ServerEndpointTrackerV2>();
            }
            else
            {
                services.AddSingleton<ServerEndpointTracker>();
                services.AddSingletonForwarder<IServerEndpointTracker, ServerEndpointTracker>();
                services.AddHostedServiceForwarder<ServerEndpointTracker>();
            }

            services.AddSingleton<ServerEndpointInfoSource>();
            services.AddHostedServiceForwarder<ServerEndpointInfoSource>();

            return services;
        }

        public static void AddScopedForwarder<TService, TImplementation>(this IServiceCollection services) where TImplementation : class, TService where TService : class
        {
            services.AddScoped<TService, TImplementation>(sp => sp.GetRequiredService<TImplementation>());
        }

        private static void AddSingletonForwarder<TService, TImplementation>(this IServiceCollection services) where TImplementation : class, TService where TService : class
        {
            services.AddSingleton<TService, TImplementation>(sp => sp.GetRequiredService<TImplementation>());
        }

        public static void AddHostedServiceForwarder<THostedService>(this IServiceCollection services) where THostedService : class, IHostedService
        {
            services.AddHostedService<THostedService>(sp => sp.GetRequiredService<THostedService>());
        }
    }
}
