// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Validation;
using Microsoft.AspNetCore.Http.Validation.Generated;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers.EventCounterShortcuts;
using Microsoft.Diagnostics.Tools.Monitor.Egress.FileSystem;
using Microsoft.Diagnostics.Tools.Monitor.Extensibility;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    // The Validation source generator doesn't run for libraries that don't call AddValidation,
    // so we can't generate IValidatableInfo by using [ValidatableType] directly on types defined
    // in ProjectReferences. This is a workaround to force the generator running in this project to
    // generate IValidatableInfo for the referenced types. The containing class is not used otherwise.
    [ValidatableType]
    internal class ValidatableTypes
    {
        public required MetricsOptions MetricsOptions { get; init; }

        public required AuthenticationOptions AuthenticationOptions { get; init; }

        public required CollectionRuleOptions CollectionRuleOptions { get; init; }

        public required CollectTraceOptions CollectTraceOptions { get; init; }

        public required ExecuteOptions ExecuteOptions { get; init; }

        public required SetEnvironmentVariableOptions SetEnvironmentVariableOptions { get; init; }

        public required GetEnvironmentVariableOptions GetEnvironmentVariableOptions { get; init; }
        
        public required GlobalCounterOptions GlobalCounterOptions { get; init; }

        public required CollectGCDumpOptions CollectGCDumpOptions { get; init; }

        public required CollectLiveMetricsOptions CollectLiveMetricsOptions { get; init; }

        public required CollectStacksOptions CollectStacksOptions { get; init; }

        public required CollectLogsOptions CollectLogsOptions { get; init; }

        public required RootOptions RootOptions { get; init; }

        public required FileSystemEgressProviderOptions FileSystemEgressProviderOptions { get; init; }

        public required CollectDumpOptions CollectDumpOptions { get; init; }

        public required LoadProfilerOptions LoadProfilerOptions { get; init; }

        public required CollectExceptionsOptions CollectExceptionsOptions { get; init; }

        // Triggers...
        public required AspNetRequestCountOptions AspNetRequestCountOptions { get; init; }
        public required AspNetRequestDurationOptions AspNetRequestDurationOptions { get; init; }
        public required AspNetResponseStatusOptions AspNetResponseStatusOptions { get; init; }
        public required EventCounterOptions EventCounterOptions { get; init; }
        public required CPUUsageOptions CPUUsageOptions { get; init; }
        public required GCHeapSizeOptions GCHeapSizeOptions { get; init; }
        public required ThreadpoolQueueLengthOptions ThreadpoolQueueLengthOptions { get; init; }
        public required EventMeterOptions EventMeterOptions { get; init; }

        // Nested member
        public required EventPipeProvider EventPipeProvider { get; init; }

        public required ExtensionManifest ExtensionManifest { get; init; }

        // public RootOptions RootOptions { get; init; } // TODO: this hits bad generated code.
        // Take a more granular approach for now.


        public required AzureAdOptions AzureAdOptions { get; init; }

        // TODO: only one resolver per project? Generate this for tests, for now. Maybe want to separate this one out
        // by test later.
        public static void AddValidation(IServiceCollection services)
        {
            GeneratedServiceCollectionExtensions.AddValidation(services);
            ValidationServiceCollectionExtensions.AddValidation(services, options =>
            {
                options.Resolvers.Insert(0, new CustomValidatableInfoResolver());
            });
        }
    }
}
