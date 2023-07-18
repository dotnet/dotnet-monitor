// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.StartupHook.MonitorMessageDispatcher.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing
{
    public sealed class MethodResolver
    {
        private sealed class DeclaringTypeDescription : IEquatable<DeclaringTypeDescription?>
        {
            public string ModuleName { get; set; } = string.Empty;
            public string ClassName { get; set; } = string.Empty;

            public override bool Equals(object? obj)
            {
                return Equals(obj as DeclaringTypeDescription);
            }

            public bool Equals(DeclaringTypeDescription? other)
            {
                return other is not null &&
                       ModuleName == other.ModuleName &&
                       ClassName == other.ClassName;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(ModuleName, ClassName);
            }
        }

        private readonly Dictionary<string, List<Module>> _dllNameToModules = new(StringComparer.InvariantCultureIgnoreCase);
        private readonly Dictionary<DeclaringTypeDescription, List<MethodInfo>> _declaringTypeToMethods = new();

        public MethodResolver()
        {
            // Build a lookup table of all viable dll names to their backing reflection Module.
            IEnumerable<Assembly> assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(
                assembly => !assembly.ReflectionOnly &&
                !assembly.IsDynamic);

            foreach (Assembly assembly in assemblies)
            {
                foreach (Module module in assembly.GetModules())
                {
                    if (!_dllNameToModules.TryGetValue(module.Name, out List<Module>? moduleList))
                    {
                        moduleList = new List<Module>();
                        _dllNameToModules[module.Name] = moduleList;
                    }

                    moduleList.Add(module);
                }
            }
        }

        public List<MethodInfo> ResolveMethodDescription(MethodDescription methodDescription)
        {
            // A single description can match multiple MethodInfos
            List<MethodInfo> matchingMethods = new();

            List<MethodInfo> possibleMethods = GetMethodsForDeclaringType(methodDescription);
            foreach (MethodInfo method in possibleMethods)
            {
                if (method.Name == methodDescription.MethodName)
                {
                    matchingMethods.Add(method);
                }
            }

            return matchingMethods;
        }

        private List<MethodInfo> GetMethodsForDeclaringType(MethodDescription methodDescription)
        {
            // Maintain a cache for all methods for a given module+class.
            DeclaringTypeDescription declType = new()
            {
                ModuleName = methodDescription.ModuleName,
                ClassName = methodDescription.ClassName
            };

            if (_declaringTypeToMethods.TryGetValue(declType, out List<MethodInfo>? methods))
            {
                return methods;
            }

            List<MethodInfo> classMethods = new();
            if (!_dllNameToModules.TryGetValue(methodDescription.ModuleName, out List<Module>? possibleModules))
            {
                _declaringTypeToMethods.Add(declType, classMethods);
                return classMethods;
            }

            foreach (Module module in possibleModules)
            {
                try
                {
                    MethodInfo[]? allMethods = module.Assembly.GetType(methodDescription.ClassName)?.GetMethods(
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.Instance |
                        BindingFlags.Static |
                        BindingFlags.FlattenHierarchy);

                    if (allMethods == null)
                    {
                        continue;
                    }

                    classMethods.AddRange(allMethods);
                }
                catch
                {
                    // CONSIDER: Are there certain exceptions we don't want to swallow here?
                }
            }

            _declaringTypeToMethods.Add(declType, classMethods);
            return classMethods;
        }
    }
}
