// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.ObjectFormatting
{
    [DebuggerDisplay("Count = {_cache.Count}")]
    internal sealed class ObjectFormatterCache
    {
        private readonly ConcurrentDictionary<Type, ObjectFormatterFunc> _cache = new();

        public ObjectFormatterCache() { }

        public void CacheMethodParameters(MethodInfo method)
        {
            if (method.HasImplicitThis() && method.DeclaringType != null)
            {
                _ = GetFormatter(method.DeclaringType);
            }

            ParameterInfo[] parameters = method.GetParameters();
            foreach (ParameterInfo parameter in parameters)
            {
                if (parameter.ParameterType.IsInterface)
                {
                    // No point in caching interfaces, GetFormatter should only get called with concrete implementations
                    continue;
                }
                _ = GetFormatter(parameter.ParameterType);
            }
        }

        public ObjectFormatterFunc GetFormatter(Type objType)
        {
            if (_cache.TryGetValue(objType, out ObjectFormatterFunc? formatter) && formatter != null)
            {
                return formatter;
            }

            FormatterFactoryResult factoryResult = ObjectFormatterFactory.GetFormatter(objType);
            foreach (Type type in factoryResult.MatchingTypes)
            {
                _cache[type] = factoryResult.Formatter;
            }

            return factoryResult.Formatter;
        }

        public void Clear()
        {
            _cache.Clear();
        }
    }
}
