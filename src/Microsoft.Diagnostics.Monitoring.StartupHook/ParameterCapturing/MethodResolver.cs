// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.StartupHook.MonitorMessageDispatcher.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing
{
    internal sealed class MethodResolver
    {
        private record DeclaringTypeDescription(string ModuleName, string TypeName);

        private readonly Dictionary<string, List<Module>> _nameToModules = new(StringComparer.Ordinal);
        private readonly Dictionary<DeclaringTypeDescription, List<MethodInfo>> _declaringTypeToMethods = new();

        public MethodResolver()
        {
            // Build a lookup table of all viable module names to their backing reflection object.
            IEnumerable<Assembly> assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(
                assembly => !assembly.ReflectionOnly &&
                !assembly.IsDynamic);

            foreach (Assembly assembly in assemblies)
            {
                foreach (Module module in assembly.GetModules())
                {
                    if (_nameToModules.TryGetValue(module.Name, out List<Module>? moduleList))
                    {
                        moduleList.Add(module);
                    }
                    else
                    {
                        _nameToModules[module.Name] = new List<Module>()
                        {
                            module
                        };
                    }
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
            // Maintain a cache for all methods for a given module+type.
            DeclaringTypeDescription declType = new(methodDescription.ModuleName, methodDescription.TypeName);
            if (_declaringTypeToMethods.TryGetValue(declType, out List<MethodInfo>? methods))
            {
                return methods;
            }

            List<MethodInfo> declaringTypeMethods = new();
            if (_nameToModules.TryGetValue(methodDescription.ModuleName, out List<Module>? possibleModules))
            {
                foreach (Module module in possibleModules)
                {
                    try
                    {
                        IEnumerable<MethodInfo>? allMethods = module.Assembly.GetType(methodDescription.TypeName)?.GetMethods(
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.Instance |
                            BindingFlags.Static)
                            .Where(method => !method.IsSpecialName);

                        if (allMethods == null)
                        {
                            continue;
                        }

                        declaringTypeMethods.AddRange(allMethods);
                    }
                    catch
                    {
                        // CONSIDER: Are there certain exceptions we don't want to swallow here?
                    }
                }
            }

            _declaringTypeToMethods.Add(declType, declaringTypeMethods);
            return declaringTypeMethods;
        }
    }
}
