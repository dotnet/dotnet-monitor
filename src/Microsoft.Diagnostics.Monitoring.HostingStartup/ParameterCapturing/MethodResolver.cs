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
        private record DeclaringTypeDescription(string AssemblyName, string TypeName);

        private readonly Dictionary<string, List<Assembly>> _nameToAssemblies = new(StringComparer.InvariantCultureIgnoreCase);
        private readonly Dictionary<DeclaringTypeDescription, List<MethodInfo>> _declaringTypeToMethods = new();

        public MethodResolver()
        {
            // Build a lookup table of all viable assembly names to their backing reflection Assembly.
            IEnumerable<Assembly> assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(
                assembly => !assembly.ReflectionOnly &&
                !assembly.IsDynamic);

            foreach (Assembly assembly in assemblies)
            {
                string? assemblySimpleName = assembly.GetName().Name;
                if (assemblySimpleName == null)
                {
                    continue;
                }

                if (!_nameToAssemblies.TryGetValue(assemblySimpleName, out List<Assembly>? assemblyList))
                {
                    _nameToAssemblies[assemblySimpleName] = new List<Assembly>()
                    {
                        assembly
                    };
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
            // Maintain a cache for all methods for a given assembly+type.
            DeclaringTypeDescription declType = new(methodDescription.AssemblyName, methodDescription.TypeName);
            if (_declaringTypeToMethods.TryGetValue(declType, out List<MethodInfo>? methods))
            {
                return methods;
            }

            List<MethodInfo> classMethods = new();
            if (!_nameToAssemblies.TryGetValue(methodDescription.AssemblyName, out List<Assembly>? possibleAssemblies))
            {
                _declaringTypeToMethods.Add(declType, classMethods);
                return classMethods;
            }

            foreach (Assembly assembly in possibleAssemblies)
            {
                try
                {
                    MethodInfo[]? allMethods = assembly.GetType(methodDescription.TypeName)?.GetMethods(
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.Instance |
                        BindingFlags.Static);

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
