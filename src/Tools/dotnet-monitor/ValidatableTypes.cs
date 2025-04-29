// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Validation;
using Microsoft.AspNetCore.Http.Validation.Generated;
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
    // We use the same pattern for all types, including those defined in the same project.
    [ValidatableType]
    internal class ValidatableTypes
    {
        public required RootOptions RootOptions { get; init; }


        // Action options
        public required CollectDumpOptions CollectDumpOptions { get; init; }
        public required CollectExceptionsOptions CollectExceptionsOptions { get; init; }
        public required CollectGCDumpOptions CollectGCDumpOptions { get; init; }
        public required CollectLiveMetricsOptions CollectLiveMetricsOptions { get; init; }
        public required CollectLogsOptions CollectLogsOptions { get; init; }
        public required CollectStacksOptions CollectStacksOptions { get; init; }
        public required CollectTraceOptions CollectTraceOptions { get; init; }
        // Necessary to work around the generated validation code not recursing into List<T>? members:
        // https://github.com/dotnet/aspnetcore/issues/61737
        public required EventPipeProvider EventPipeProvider { get; init; }
        public required ExecuteOptions ExecuteOptions { get; init; }
        public required GetEnvironmentVariableOptions GetEnvironmentVariableOptions { get; init; }
        public required LoadProfilerOptions LoadProfilerOptions { get; init; }
        public required SetEnvironmentVariableOptions SetEnvironmentVariableOptions { get; init; }


        // Trigger options
        // EventCounterShortcuts
        public required CPUUsageOptions CPUUsageOptions { get; init; }
        public required GCHeapSizeOptions GCHeapSizeOptions { get; init; }
        public required ThreadpoolQueueLengthOptions ThreadpoolQueueLengthOptions { get; init; }
        // Other trigger options
        public required AspNetRequestCountOptions AspNetRequestCountOptions { get; init; }
        public required AspNetRequestDurationOptions AspNetRequestDurationOptions { get; init; }
        public required AspNetResponseStatusOptions AspNetResponseStatusOptions { get; init; }
        public required EventCounterOptions EventCounterOptions { get; init; }
        public required EventMeterOptions EventMeterOptions { get; init; }


        public required FileSystemEgressProviderOptions FileSystemEgressProviderOptions { get; init; }
        public required ExtensionManifest ExtensionManifest { get; init; }

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
