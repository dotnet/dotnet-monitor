// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Identification
{
    /// <summary>
    /// Class that caches naming information for provided metadata
    /// and invokes callbacks for new instances of the metadata.
    /// </summary>
    internal sealed class ExceptionGroupIdentifierCache
    {
        // List of callbacks to invoke for newly provided metadata.
        private readonly IEnumerable<ExceptionGroupIdentifierCacheCallback> _callbacks;

        // Mapping of ExceptionGroupIdentifier to a unique ID; ExceptionGroupIdentifier itself does not have a way
        // of uniquely identifying itself using a primitive type.
        private readonly ConcurrentDictionary<ExceptionGroupIdentifier, ulong> _exceptionGroupIds = new();

        private readonly ConcurrentDictionary<MethodBase, ulong> _methodIds = new();

        // Mapping of Module to a unique ID; Module itself does not have a way
        // of uniquely identifying itself using a primitive type.
        private readonly ConcurrentDictionary<Module, ulong> _moduleIds = new();

        private readonly ConcurrentDictionary<StackFrameIdentifier, ulong> _stackFrameIds = new();

        // Name cache for all provided metadata
        private readonly NameCache _nameCache = new();

        private ulong _nextGroupId = 1;
        private ulong _nextMethodId = 1;
        private ulong _nextModuleId = 1;
        private ulong _nextStackFrameId = 1;

        public ExceptionGroupIdentifierCache(IEnumerable<ExceptionGroupIdentifierCacheCallback> callbacks)
        {
            ArgumentNullException.ThrowIfNull(callbacks);

            _callbacks = callbacks;
        }

        /// <summary>
        /// Gets the identifier of the <see cref="ExceptionGroupIdentifier"/> and
        /// caches its data if it has not been encountered yet.
        /// </summary>
        /// <returns>
        /// An identifier that uniquely identifies the <see cref="ExceptionGroupIdentifier"/> in this cache.
        /// </returns>
        public ulong GetOrAdd(ExceptionGroupIdentifier exceptionGroupId)
        {
            if (!GetOrCreateIdentifier(_exceptionGroupIds, exceptionGroupId, ref _nextGroupId, out ulong groupId))
                return groupId;

            ExceptionGroupData data = new()
            {
                ExceptionClassId = GetOrAdd(exceptionGroupId.ExceptionType),
                ThrowingMethodId = AddOrDefault(exceptionGroupId.ThrowingMethod),
                ILOffset = exceptionGroupId.ILOffset
            };

            InvokeExceptionGroupCallbacks(groupId, data);

            return groupId;
        }

        /// <summary>
        /// Gets the identifier of the <see cref="MethodBase"/> instance and
        /// caches its data if it has not been encountered yet.
        /// </summary>
        /// <returns>
        /// An identifier that uniquely identifies the <see cref="MethodBase"/> in this cache.
        /// </returns>
        public ulong GetOrAdd(MethodBase method)
        {
            if (!GetOrCreateIdentifier(_methodIds, method, ref _nextMethodId, out ulong methodId))
                return methodId;

            // Dynamic methods do not have metadata tokens
            uint methodToken = 0;
            try
            {
                methodToken = Convert.ToUInt32(method.MetadataToken);
            }
            catch (Exception) { }

            uint parentClassToken = 0;
            if (null != method.DeclaringType)
            {
                parentClassToken = Convert.ToUInt32(method.DeclaringType.MetadataToken);
            }

            bool stackTraceHidden = false;
            try
            {
                stackTraceHidden = method.GetCustomAttribute<StackTraceHiddenAttribute>(inherit: false) != null;
            }
            catch (Exception) { }

            // RTDynamicMethod does not implement GetGenericArguments.
            Type[] genericArguments = Array.Empty<Type>();
            try
            {
                genericArguments = method.GetGenericArguments();
            }
            catch (Exception) { }

            FunctionData data = new(
                method.Name,
                methodToken,
                AddOrDefault(method.DeclaringType),
                parentClassToken,
                GetOrAdd(method.Module),
                GetOrAdd(genericArguments),
                GetOrAdd(method.GetParameters()),
                stackTraceHidden);

            if (_nameCache.FunctionData.TryAdd(methodId, data))
            {
                InvokeFunctionDataCallbacks(methodId, data);
            }

            return methodId;
        }


        /// <summary>
        /// Gets the identifier of the <see cref="Module"/> instance and
        /// caches its data if it has not been encountered yet.
        /// </summary>
        /// <returns>
        /// An identifier that uniquely identifies the <see cref="Module"/> in this cache.
        /// </returns>
        public ulong GetOrAdd(Module module)
        {
            if (!GetOrCreateIdentifier(_moduleIds, module, ref _nextModuleId, out ulong moduleId))
                return moduleId;

            ModuleData data = new(module.Name, module.ModuleVersionId);

            if (_nameCache.ModuleData.TryAdd(moduleId, data))
            {
                InvokeModuleDataCallbacks(moduleId, data);
            }

            return moduleId;
        }

        /// <summary>
        /// Gets the identifier of the <see cref="StackFrame"/> instance and
        /// caches its data if it has not been encountered yet.
        /// </summary>
        /// <returns>
        /// An identifier that uniquely identifies the <see cref="StackFrame"/> in this cache.
        /// </returns>
        public ulong GetOrAdd(StackFrame frame)
        {
            StackFrameIdentifier identifier = new(
                AddOrDefault(frame.GetMethod()),
                frame.GetILOffset());

            if (!GetOrCreateIdentifier(_stackFrameIds, identifier, ref _nextStackFrameId, out ulong frameId))
                return frameId;

            StackFrameData data = new()
            {
                MethodId = identifier.MethodId,
                ILOffset = identifier.ILOffset
            };

            InvokeStackFrameDataCallbacks(frameId, data);

            return frameId;
        }

        public ulong[] GetOrAdd(ReadOnlySpan<StackFrame> frames)
        {
            ulong[] frameIds;
            if (frames.Length > 0)
            {
                frameIds = new ulong[frames.Length];
                for (int i = 0; i < frames.Length; i++)
                {
                    frameIds[i] = GetOrAdd(frames[i]);
                }
            }
            else
            {
                frameIds = Array.Empty<ulong>();
            }
            return frameIds;
        }

        /// <summary>
        /// Gets the identifier of the <see cref="Type"/> instance and
        /// caches its data if it has not been encountered yet.
        /// </summary>
        /// <returns>
        /// An identifier that uniquely identifies the <see cref="Type"/> in this cache.
        /// </returns>
        public ulong GetOrAdd(Type type)
        {
            ulong originalId = GetId(type);
            ulong classId = originalId;
            while (!_nameCache.ClassData.ContainsKey(classId))
            {
                ulong moduleId = GetOrAdd(type.Module);
                uint typeToken = Convert.ToUInt32(type.MetadataToken);

                bool stackTraceHidden = false;
                try
                {
                    stackTraceHidden = type.GetCustomAttribute<StackTraceHiddenAttribute>(inherit: false) != null;
                }
                catch (Exception) { }

                ClassData classData = new(
                    typeToken,
                    moduleId,
                    ClassFlags.None,
                    GetOrAdd(type.GetGenericArguments()),
                    stackTraceHidden);

                if (!_nameCache.ClassData.TryAdd(classId, classData))
                    break;

                InvokeClassDataCallbacks(classId, classData);

                ModuleScopedToken key = new(moduleId, typeToken);
                if (!_nameCache.TokenData.ContainsKey(key))
                {
                    uint parentClassToken = 0;
                    if (null != type.DeclaringType)
                    {
                        parentClassToken = Convert.ToUInt32(type.DeclaringType.MetadataToken);
                    }

                    TokenData tokenData = new(
                        type.Name,
                        null == type.DeclaringType ? type.Namespace ?? string.Empty : string.Empty,
                        parentClassToken,
                        stackTraceHidden);

                    if (!_nameCache.TokenData.TryAdd(key, tokenData))
                        break;

                    InvokeTokenDataCallbacks(moduleId, typeToken, tokenData);
                }

                if (null == type.DeclaringType)
                {
                    break;
                }
                else
                {
                    type = type.DeclaringType;
                    classId = GetId(type);
                }
            }
            return originalId;
        }

        private ulong[] GetOrAdd(Type[] types)
        {
            ulong[] classIds;
            if (types.Length > 0)
            {
                classIds = new ulong[types.Length];
                for (int i = 0; i < types.Length; i++)
                {
                    classIds[i] = GetOrAdd(types[i]);
                }
            }
            else
            {
                classIds = Array.Empty<ulong>();
            }
            return classIds;
        }

        private ulong[] GetOrAdd(ParameterInfo[] parameters)
        {
            ulong[] classIds;
            if (parameters.Length > 0)
            {
                classIds = new ulong[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    classIds[i] = GetOrAdd(parameters[i].ParameterType);
                }
            }
            else
            {
                classIds = Array.Empty<ulong>();
            }
            return classIds;
        }

        private ulong AddOrDefault(MethodBase? method)
        {
            if (null == method)
                return default;

            return GetOrAdd(method);
        }

        private ulong AddOrDefault(Type? type)
        {
            if (null == type)
                return default;

            return GetOrAdd(type);
        }

        private ulong GetId(Module module)
        {
            return _moduleIds.GetOrAdd(module, _ => _nextModuleId++);
        }

        private static ulong GetId(Type type)
        {
            return Convert.ToUInt64(type.TypeHandle.Value.ToInt64());
        }

        /// <summary>
        /// Gets the current identifier for the <paramref name="key"/> or creates a new
        /// identifier from <paramref name="nextIdentifier"/>.
        /// </summary>
        /// <param name="dictionary">The dictionary that contains the mapping from keys to identifiers.</param>
        /// <param name="key">The value to find in the dictionary or to insert if it doesn't exist.</param>
        /// <param name="nextIdentifier">Reference that tracks the next usable identifier.</param>
        /// <param name="identifier">The identifier associated with the <paramref name="key"/> value.</param>
        /// <returns>True if newly inserted into dictionary; otherwise false.</returns>
        /// <remarks>
        /// This method guarantees that a given <paramref name="key"/> will always have one unique identifier;
        /// it ensures that concurrent inserts into the dictionary are resolved to have the same reported identifier.
        /// </remarks>
        private static bool GetOrCreateIdentifier<T>(
            ConcurrentDictionary<T, ulong> dictionary,
            T key,
            ref ulong nextIdentifier,
            out ulong identifier) where T : notnull
        {
            if (dictionary.TryGetValue(key, out identifier))
                return false;

            // Lock against multiple threads attempting to create the identifier for the key. If the threads are acting
            // for different keys, the assigned identifier value being different is desired. If the threads are acting for
            // the same keys, its okay if multiple identifiers are created for the same key; it will be reconciled when attempting
            // to add the identifier to the dictionary.
            identifier = Interlocked.Increment(ref nextIdentifier);

            // If multiple threads try to GetOrAdd for the same key at the same time, one of them will do the
            // actual add and the others will return immediately. The return value indicates the value in the dictionary
            // that corresponds to the key. Compare this returned value to the one that the thread tried to add; if they
            // are the same, then the thread is the one that actually added the value to the dictionary.
            ulong actualIdentifier = dictionary.GetOrAdd(key, identifier);

            // If the candidate identifier is the same as the actual value from the dictionary, then this thread
            // was responsible for inserting the identifier. Otherwise, some other concurrent thread inserted the identifier.
            if (actualIdentifier == identifier)
            {
                return true;
            }

            identifier = actualIdentifier;
            return false;
        }

        private void InvokeExceptionGroupCallbacks(ulong groupId, ExceptionGroupData data)
        {
            foreach (ExceptionGroupIdentifierCacheCallback callback in _callbacks)
            {
                callback.OnExceptionGroupData(groupId, data);
            }
        }

        private void InvokeClassDataCallbacks(ulong classId, ClassData data)
        {
            foreach (ExceptionGroupIdentifierCacheCallback callback in _callbacks)
            {
                callback.OnClassData(classId, data);
            }
        }

        private void InvokeFunctionDataCallbacks(ulong functionId, FunctionData data)
        {
            foreach (ExceptionGroupIdentifierCacheCallback callback in _callbacks)
            {
                callback.OnFunctionData(functionId, data);
            }
        }

        private void InvokeModuleDataCallbacks(ulong moduleId, ModuleData data)
        {
            foreach (ExceptionGroupIdentifierCacheCallback callback in _callbacks)
            {
                callback.OnModuleData(moduleId, data);
            }
        }

        private void InvokeStackFrameDataCallbacks(ulong moduleId, StackFrameData data)
        {
            foreach (ExceptionGroupIdentifierCacheCallback callback in _callbacks)
            {
                callback.OnStackFrameData(moduleId, data);
            }
        }

        private void InvokeTokenDataCallbacks(ulong moduleId, uint typeToken, TokenData data)
        {
            foreach (ExceptionGroupIdentifierCacheCallback callback in _callbacks)
            {
                callback.OnTokenData(moduleId, typeToken, data);
            }
        }
    }
}
