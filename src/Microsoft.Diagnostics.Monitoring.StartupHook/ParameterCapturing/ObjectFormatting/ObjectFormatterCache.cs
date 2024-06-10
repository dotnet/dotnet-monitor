// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing.ObjectFormatting
{
    [DebuggerDisplay("Count = {_cache.Count}, UseDebuggerDisplayAttribute={_useDebuggerDisplayAttribute}")]
    internal sealed class ObjectFormatterCache
    {
        private readonly ConcurrentDictionary<Type, ObjectFormatterFunc> _cache = new();
        private readonly bool _useDebuggerDisplayAttribute;

        public ObjectFormatterCache(bool useDebuggerDisplayAttribute)
        {
            _useDebuggerDisplayAttribute = useDebuggerDisplayAttribute;
        }

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

        public bool TryGetFormatter(Type objType, out ObjectFormatterFunc? formatter)
        {
            return _cache.TryGetValue(objType, out formatter);
        }

        public ObjectFormatterFunc GetFormatter(Type objType)
        {
            if (_cache.TryGetValue(objType, out ObjectFormatterFunc? formatter) && formatter != null)
            {
                return formatter;
            }

            FormatterFactoryResult factoryResult = ObjectFormatterFactory.GetFormatter(objType, _useDebuggerDisplayAttribute);
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
