#nullable enable annotations
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
#nullable enable

namespace System.Runtime.CompilerServices
{
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.AspNetCore.Http.ValidationsGenerator, Version=10.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60", "10.0.0.0")]
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    file sealed class InterceptsLocationAttribute : System.Attribute
    {
        public InterceptsLocationAttribute(int version, string data)
        {
        }
    }
}

namespace Microsoft.AspNetCore.Http.Validation.Generated
{
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.AspNetCore.Http.ValidationsGenerator, Version=10.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60", "10.0.0.0")]
    file sealed class GeneratedValidatablePropertyInfo : global::Microsoft.AspNetCore.Http.Validation.ValidatablePropertyInfo
    {
        public GeneratedValidatablePropertyInfo(
            [param: global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)]
            global::System.Type containingType,
            global::System.Type propertyType,
            string name,
            string displayName) : base(containingType, propertyType, name, displayName)
        {
            ContainingType = containingType;
            Name = name;
        }

        [global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)]
        internal global::System.Type ContainingType { get; }
        internal string Name { get; }

        protected override global::System.ComponentModel.DataAnnotations.ValidationAttribute[] GetValidationAttributes()
            => ValidationAttributeCache.GetValidationAttributes(ContainingType, Name);
    }

    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.AspNetCore.Http.ValidationsGenerator, Version=10.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60", "10.0.0.0")]
    file sealed class GeneratedValidatableTypeInfo : global::Microsoft.AspNetCore.Http.Validation.ValidatableTypeInfo
    {
        public GeneratedValidatableTypeInfo(
            [param: global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.Interfaces)]
            global::System.Type type,
            ValidatablePropertyInfo[] members) : base(type, members) { }
    }

    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.AspNetCore.Http.ValidationsGenerator, Version=10.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60", "10.0.0.0")]
    file class GeneratedValidatableInfoResolver : global::Microsoft.AspNetCore.Http.Validation.IValidatableInfoResolver
    {
        public bool TryGetValidatableTypeInfo(global::System.Type type, [global::System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out global::Microsoft.AspNetCore.Http.Validation.IValidatableInfo? validatableInfo)
        {
            validatableInfo = null;
            if (type == typeof(global::Microsoft.Diagnostics.Tools.Monitor.Extensibility.ExtensionManifest))
            {
                validatableInfo = CreateExtensionManifest();
                return true;
            }
            if (type == typeof(global::Microsoft.Diagnostics.Monitoring.WebApi.MetricProvider))
            {
                validatableInfo = CreateMetricProvider();
                return true;
            }
            if (type == typeof(global::Microsoft.Diagnostics.Monitoring.WebApi.MetricsOptions))
            {
                validatableInfo = CreateMetricsOptions();
                return true;
            }
            if (type == typeof(global::Microsoft.Diagnostics.Tools.Monitor.MonitorApiKeyOptions))
            {
                validatableInfo = CreateMonitorApiKeyOptions();
                return true;
            }
            if (type == typeof(global::Microsoft.Diagnostics.Tools.Monitor.AzureAdOptions))
            {
                validatableInfo = CreateAzureAdOptions();
                return true;
            }
            if (type == typeof(global::Microsoft.Diagnostics.Tools.Monitor.AuthenticationOptions))
            {
                validatableInfo = CreateAuthenticationOptions();
                return true;
            }
            if (type == typeof(global::Microsoft.Diagnostics.Tools.Monitor.ValidatableTypes))
            {
                validatableInfo = CreateValidatableTypes();
                return true;
            }

            return false;
        }

        // No-ops, rely on runtime code for ParameterInfo-based resolution
        public bool TryGetValidatableParameterInfo(global::System.Reflection.ParameterInfo parameterInfo, [global::System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out global::Microsoft.AspNetCore.Http.Validation.IValidatableInfo? validatableInfo)
        {
            validatableInfo = null;
            return false;
        }

        private ValidatableTypeInfo CreateExtensionManifest()
        {
            return new GeneratedValidatableTypeInfo(
                type: typeof(global::Microsoft.Diagnostics.Tools.Monitor.Extensibility.ExtensionManifest),
                members: [
                    new GeneratedValidatablePropertyInfo(
                        containingType: typeof(global::Microsoft.Diagnostics.Tools.Monitor.Extensibility.ExtensionManifest),
                        propertyType: typeof(string),
                        name: "Name",
                        displayName: "Name"
                    ),
                ]
            );
        }
        private ValidatableTypeInfo CreateMetricProvider()
        {
            return new GeneratedValidatableTypeInfo(
                type: typeof(global::Microsoft.Diagnostics.Monitoring.WebApi.MetricProvider),
                members: [
                    new GeneratedValidatablePropertyInfo(
                        containingType: typeof(global::Microsoft.Diagnostics.Monitoring.WebApi.MetricProvider),
                        propertyType: typeof(string),
                        name: "ProviderName",
                        displayName: "Microsoft.Diagnostics.Monitoring.WebApi.OptionsDisplayStrings"
                    ),
                ]
            );
        }
        private ValidatableTypeInfo CreateMetricsOptions()
        {
            return new GeneratedValidatableTypeInfo(
                type: typeof(global::Microsoft.Diagnostics.Monitoring.WebApi.MetricsOptions),
                members: [
                    new GeneratedValidatablePropertyInfo(
                        containingType: typeof(global::Microsoft.Diagnostics.Monitoring.WebApi.MetricsOptions),
                        propertyType: typeof(int?),
                        name: "MetricCount",
                        displayName: "Microsoft.Diagnostics.Monitoring.WebApi.OptionsDisplayStrings"
                    ),
                    new GeneratedValidatablePropertyInfo(
                        containingType: typeof(global::Microsoft.Diagnostics.Monitoring.WebApi.MetricsOptions),
                        propertyType: typeof(global::System.Collections.Generic.List<global::Microsoft.Diagnostics.Monitoring.WebApi.MetricProvider>),
                        name: "Providers",
                        displayName: "Microsoft.Diagnostics.Monitoring.WebApi.OptionsDisplayStrings"
                    ),
                ]
            );
        }
        private ValidatableTypeInfo CreateMonitorApiKeyOptions()
        {
            return new GeneratedValidatableTypeInfo(
                type: typeof(global::Microsoft.Diagnostics.Tools.Monitor.MonitorApiKeyOptions),
                members: [
                    new GeneratedValidatablePropertyInfo(
                        containingType: typeof(global::Microsoft.Diagnostics.Tools.Monitor.MonitorApiKeyOptions),
                        propertyType: typeof(string),
                        name: "Subject",
                        displayName: "Microsoft.Diagnostics.Monitoring.WebApi.OptionsDisplayStrings"
                    ),
                    new GeneratedValidatablePropertyInfo(
                        containingType: typeof(global::Microsoft.Diagnostics.Tools.Monitor.MonitorApiKeyOptions),
                        propertyType: typeof(string),
                        name: "PublicKey",
                        displayName: "Microsoft.Diagnostics.Monitoring.WebApi.OptionsDisplayStrings"
                    ),
                ]
            );
        }
        private ValidatableTypeInfo CreateAzureAdOptions()
        {
            return new GeneratedValidatableTypeInfo(
                type: typeof(global::Microsoft.Diagnostics.Tools.Monitor.AzureAdOptions),
                members: [
                    new GeneratedValidatablePropertyInfo(
                        containingType: typeof(global::Microsoft.Diagnostics.Tools.Monitor.AzureAdOptions),
                        propertyType: typeof(string),
                        name: "TenantId",
                        displayName: "Microsoft.Diagnostics.Monitoring.WebApi.OptionsDisplayStrings"
                    ),
                    new GeneratedValidatablePropertyInfo(
                        containingType: typeof(global::Microsoft.Diagnostics.Tools.Monitor.AzureAdOptions),
                        propertyType: typeof(string),
                        name: "ClientId",
                        displayName: "Microsoft.Diagnostics.Monitoring.WebApi.OptionsDisplayStrings"
                    ),
                    new GeneratedValidatablePropertyInfo(
                        containingType: typeof(global::Microsoft.Diagnostics.Tools.Monitor.AzureAdOptions),
                        propertyType: typeof(global::System.Uri),
                        name: "AppIdUri",
                        displayName: "Microsoft.Diagnostics.Monitoring.WebApi.OptionsDisplayStrings"
                    ),
                    new GeneratedValidatablePropertyInfo(
                        containingType: typeof(global::Microsoft.Diagnostics.Tools.Monitor.AzureAdOptions),
                        propertyType: typeof(string),
                        name: "RequiredRole",
                        displayName: "Microsoft.Diagnostics.Monitoring.WebApi.OptionsDisplayStrings"
                    ),
                ]
            );
        }
        private ValidatableTypeInfo CreateAuthenticationOptions()
        {
            return new GeneratedValidatableTypeInfo(
                type: typeof(global::Microsoft.Diagnostics.Tools.Monitor.AuthenticationOptions),
                members: [
                    new GeneratedValidatablePropertyInfo(
                        containingType: typeof(global::Microsoft.Diagnostics.Tools.Monitor.AuthenticationOptions),
                        propertyType: typeof(global::Microsoft.Diagnostics.Tools.Monitor.MonitorApiKeyOptions),
                        name: "MonitorApiKey",
                        displayName: "Microsoft.Diagnostics.Monitoring.WebApi.OptionsDisplayStrings"
                    ),
                    new GeneratedValidatablePropertyInfo(
                        containingType: typeof(global::Microsoft.Diagnostics.Tools.Monitor.AuthenticationOptions),
                        propertyType: typeof(global::Microsoft.Diagnostics.Tools.Monitor.AzureAdOptions),
                        name: "AzureAd",
                        displayName: "Microsoft.Diagnostics.Monitoring.WebApi.OptionsDisplayStrings"
                    ),
                ]
            );
        }
        private ValidatableTypeInfo CreateValidatableTypes()
        {
            return new GeneratedValidatableTypeInfo(
                type: typeof(global::Microsoft.Diagnostics.Tools.Monitor.ValidatableTypes),
                members: [
                    new GeneratedValidatablePropertyInfo(
                        containingType: typeof(global::Microsoft.Diagnostics.Tools.Monitor.ValidatableTypes),
                        propertyType: typeof(global::Microsoft.Diagnostics.Monitoring.WebApi.MetricsOptions),
                        name: "MetricsOptions",
                        displayName: "MetricsOptions"
                    ),
                    new GeneratedValidatablePropertyInfo(
                        containingType: typeof(global::Microsoft.Diagnostics.Tools.Monitor.ValidatableTypes),
                        propertyType: typeof(global::Microsoft.Diagnostics.Tools.Monitor.AuthenticationOptions),
                        name: "AuthenticationOptions",
                        displayName: "AuthenticationOptions"
                    ),
                ]
            );
        }

    }

    static class MyExtensions
    {
        public static global::Microsoft.Extensions.DependencyInjection.IServiceCollection AddValidation(this global::Microsoft.Extensions.DependencyInjection.IServiceCollection services, global::System.Action<ValidationOptions>? configureOptions = null)
        {
            return GeneratedServiceCollectionExtensions.AddValidation(services, configureOptions);
        }
    }

    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.AspNetCore.Http.ValidationsGenerator, Version=10.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60", "10.0.0.0")]
    file static class GeneratedServiceCollectionExtensions
    {
        // [global::System.Runtime.CompilerServices.InterceptsLocationAttribute(1, "M2kOaR09/X9mJ22GvhUC4+4FAABTdGFydHVwLmNz")]
        public static global::Microsoft.Extensions.DependencyInjection.IServiceCollection AddValidation(this global::Microsoft.Extensions.DependencyInjection.IServiceCollection services, global::System.Action<ValidationOptions>? configureOptions = null)
        {
            // Use non-extension method to avoid infinite recursion.
            return global::Microsoft.Extensions.DependencyInjection.ValidationServiceCollectionExtensions.AddValidation(services, options =>
            {
                options.Resolvers.Insert(0, new GeneratedValidatableInfoResolver());
                if (configureOptions is not null)
                {
                    configureOptions(options);
                }
            });
        }
    }

    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.AspNetCore.Http.ValidationsGenerator, Version=10.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60", "10.0.0.0")]
    file static class ValidationAttributeCache
    {
        private sealed record CacheKey([property: global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)] global::System.Type ContainingType, string PropertyName);
        private static readonly global::System.Collections.Concurrent.ConcurrentDictionary<CacheKey, global::System.ComponentModel.DataAnnotations.ValidationAttribute[]> _cache = new();

        public static global::System.ComponentModel.DataAnnotations.ValidationAttribute[] GetValidationAttributes(
            global::System.Type containingType,
            string propertyName)
        {
            var key = new CacheKey(containingType, propertyName);
            return _cache.GetOrAdd(key, static k =>
            {
                var property = k.ContainingType.GetProperty(k.PropertyName);
                if (property == null)
                {
                    return [];
                }

                return [.. global::System.Reflection.CustomAttributeExtensions.GetCustomAttributes<global::System.ComponentModel.DataAnnotations.ValidationAttribute>(property, inherit: true)];
            });
        }
    }
}
