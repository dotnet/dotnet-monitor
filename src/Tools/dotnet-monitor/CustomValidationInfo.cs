// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Validation;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading;

#nullable enable

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options
{
    public class CustomValidatableInfoResolver : IValidatableInfoResolver
    {
        public bool TryGetValidatableTypeInfo(Type type, [NotNullWhen(true)] out IValidatableInfo? validatableInfo)
        {
            if (type == typeof(CollectionRuleOptions))
            {
                validatableInfo = CreateCollectionRuleOptions();
                return true;
            }

            validatableInfo = null;
            return false;
        }

        public bool TryGetValidatableParameterInfo(ParameterInfo parameterInfo, [NotNullWhen(true)] out IValidatableInfo? validatableInfo)
        {
            validatableInfo = null;
            return false;
        }

        private static ValidatableTypeInfo CreateCollectionRuleOptions()
        {
            return new ShortCircuitingValidatableTypeInfo(
                type: typeof(CollectionRuleOptions),
                members: [
                    new CustomValidatablePropertyInfo(
                        containingType: typeof(CollectionRuleOptions),
                        propertyType: typeof(CollectionRuleTriggerOptions),
                        name: "Trigger",
                        displayName: "Trigger"
                    ),
                    new CustomValidatablePropertyInfo(
                        containingType: typeof(CollectionRuleOptions),
                        propertyType: typeof(List<CollectionRuleActionOptions>),
                        name: "Actions",
                        displayName: "Actions"
                    ),
                    new CustomValidatablePropertyInfo(
                        containingType: typeof(CollectionRuleOptions),
                        propertyType: typeof(CollectionRuleLimitsOptions),
                        name: "Limits",
                        displayName: "Limits"
                    ),
                ]
            );
        }

        sealed class ShortCircuitingValidatableTypeInfo : ValidatableTypeInfo
        {
            public ShortCircuitingValidatableTypeInfo(
                [param: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
                Type type,
                ValidatablePropertyInfo[] members) : base(type, members) {
                Type = type;
                Members = members;
                _membersCount = members.Length;
                _subTypes = type.GetAllImplementedTypes();
            }

            private readonly int _membersCount;
            private readonly List<Type> _subTypes;

            internal Type Type { get; }
            internal IReadOnlyList<ValidatablePropertyInfo> Members { get; }

            public override async Task ValidateAsync(object? value, ValidateContext context, CancellationToken cancellationToken)
            {
                Debug.Assert(context.ValidationContext is not null);
                if (value == null)
                {
                    return;
                }

                // Check if we've exceeded the maximum depth
                if (context.CurrentDepth >= context.ValidationOptions.MaxDepth)
                {
                    throw new InvalidOperationException(
                        $"Maximum validation depth of {context.ValidationOptions.MaxDepth} exceeded at '{context.CurrentValidationPath}' in '{Type.Name}'. " +
                        "This is likely caused by a circular reference in the object graph. " +
                        "Consider increasing the MaxDepth in ValidationOptions if deeper validation is required.");
                }

                var originalPrefix = context.CurrentValidationPath;

                try
                {
                    // Finally validate IValidatableObject if implemented
                    if (Type.ImplementsInterface(typeof(IValidatableObject)) && value is IValidatableObject validatable)
                    {
                        // Important: Set the DisplayName to the type name for top-level validations
                        // and restore the original validation context properties
                        var originalDisplayName = context.ValidationContext.DisplayName;
                        var originalMemberName = context.ValidationContext.MemberName;

                        // Set the display name to the class name for IValidatableObject validation
                        context.ValidationContext.DisplayName = Type.Name;
                        context.ValidationContext.MemberName = null;

                        var validationResults = validatable.Validate(context.ValidationContext);
                        bool hasErrors = false;
                        foreach (var validationResult in validationResults)
                        {
                            if (validationResult != ValidationResult.Success && validationResult.ErrorMessage is not null)
                            {
                                var memberName = validationResult.MemberNames.First();
                                var key = string.IsNullOrEmpty(originalPrefix) ?
                                    memberName :
                                    $"{originalPrefix}.{memberName}";

                                context.AddOrExtendValidationError(key, validationResult.ErrorMessage);
                                hasErrors = true;
                            }
                        }

                        // Restore the original validation context properties
                        context.ValidationContext.DisplayName = originalDisplayName;
                        context.ValidationContext.MemberName = originalMemberName;
                        if (hasErrors)
                        {
                            return;
                        }
                    }

                    var actualType = value.GetType();

                    // First validate members
                    for (var i = 0; i < _membersCount; i++)
                    {
                        await Members[i].ValidateAsync(value, context, cancellationToken);
                        context.CurrentValidationPath = originalPrefix;
                    }

                    // Then validate sub-types if any
                    foreach (var subType in _subTypes)
                    {
                        // Check if the actual type is assignable to the sub-type
                        // and validate it if it is
                        if (subType.IsAssignableFrom(actualType))
                        {
                            if (context.ValidationOptions.TryGetValidatableTypeInfo(subType, out var subTypeInfo))
                            {
                                await subTypeInfo.ValidateAsync(value, context, cancellationToken);
                                context.CurrentValidationPath = originalPrefix;
                            }
                        }
                    }
                }
                finally
                {
                    context.CurrentValidationPath = originalPrefix;
                }
            }
        }


        sealed class CustomValidatableTypeInfo : ValidatableTypeInfo
        {
            public CustomValidatableTypeInfo(
                [param: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
                Type type,
                ValidatablePropertyInfo[] members) : base(type, members) { }
        }

        sealed class CustomValidatablePropertyInfo : ValidatablePropertyInfo
        {
            public CustomValidatablePropertyInfo(
                [param: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
                Type containingType,
                Type propertyType,
                string name,
                string displayName) : base(containingType, propertyType, name, displayName)
            {
                ContainingType = containingType;
                Name = name;
            }

            internal Type ContainingType { get; }
            internal string Name { get; }

            protected override ValidationAttribute[] GetValidationAttributes()
                => ValidationAttributeCache.GetValidationAttributes(ContainingType, Name);
        }

        static class ValidationAttributeCache
        {
            private sealed record CacheKey(Type ContainingType, string PropertyName);
            private static readonly ConcurrentDictionary<CacheKey, ValidationAttribute[]> _cache = new();

            public static ValidationAttribute[] GetValidationAttributes(
                Type containingType,
                string propertyName)
            {
                var key = new CacheKey(containingType, propertyName);
                return _cache.GetOrAdd(key, static k =>
                {
                    var results = new List<ValidationAttribute>();

                // Get attributes from the property
                    var property = k.ContainingType.GetProperty(k.PropertyName);
                if (property != null)
                    {
                    var propertyAttributes = CustomAttributeExtensions.GetCustomAttributes<ValidationAttribute>(property, inherit: true);

                    results.AddRange(propertyAttributes);
                    }

                // Check constructors for parameters that match the property name
                // to handle record scenarios
                foreach (var constructor in k.ContainingType.GetConstructors())
                {
                    // Look for parameter with matching name (case insensitive)
                    var parameter = Enumerable.FirstOrDefault(
                        constructor.GetParameters(),
                        p => string.Equals(p.Name, k.PropertyName, global::System.StringComparison.OrdinalIgnoreCase));

                    if (parameter != null)
                    {
                        var paramAttributes = CustomAttributeExtensions.GetCustomAttributes<ValidationAttribute>(parameter, inherit: true);

                        results.AddRange(paramAttributes);

                        break;
                    }
                }

                return results.ToArray();
                });
            }
        }
    }
    
    internal static class TypeExtensions
    {
        public static List<Type> GetAllImplementedTypes([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] this Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            var implementedTypes = new List<Type>();

            // Yield all interfaces directly and indirectly implemented by this type
            foreach (var interfaceType in type.GetInterfaces())
            {
                implementedTypes.Add(interfaceType);
            }

            // Finally, walk up the inheritance chain
            var baseType = type.BaseType;
            while (baseType != null && baseType != typeof(object))
            {
                implementedTypes.Add(baseType);
                baseType = baseType.BaseType;
            }

            return implementedTypes;
        }

        public static bool ImplementsInterface(this Type type, Type interfaceType)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentNullException.ThrowIfNull(interfaceType);

            // Check if interfaceType is actually an interface
            if (!interfaceType.IsInterface)
            {
                throw new ArgumentException($"Type {interfaceType.FullName} is not an interface.", nameof(interfaceType));
            }

            return interfaceType.IsAssignableFrom(type);
        }
    }

    internal static class ValidateContextExtensions
    {
        internal static void AddOrExtendValidationError(this ValidateContext context, string key, string error)
        {
            context.ValidationErrors ??= [];

            if (context.ValidationErrors.TryGetValue(key, out var existingErrors) && !existingErrors.Contains(error))
            {
                context.ValidationErrors[key] = [.. existingErrors, error];
            }
            else
            {
                context.ValidationErrors[key] = [error];
            }
        }
    }
}
