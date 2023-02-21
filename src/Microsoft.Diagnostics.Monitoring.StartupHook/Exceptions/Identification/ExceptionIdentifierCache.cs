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
    internal sealed class ExceptionIdentifierCache
    {
        // List of callbacks to invoke for newly provided metadata.
        private readonly IEnumerable<ExceptionIdentifierCacheCallback> _callbacks;

        // Mapping of ExceptionIdentifier to a unique ID; ExceptionIdentifier itself does not have a way
        // of uniquely identifying itself using a primitive type.
        private readonly ConcurrentDictionary<ExceptionIdentifier, ulong> _exceptionIds = new();

        // Mapping of Module to a unique ID; Module itself does not have a way
        // of uniquely identifying itself using a primitive type.
        private readonly ConcurrentDictionary<Module, ulong> _moduleIds = new();

        private readonly ConcurrentDictionary<StackFrameIdentifier, ulong> _stackFrameIds = new();

        // Name cache for all provided metadata
        private readonly NameCache _nameCache = new();

        private ulong _nextRegistrationId = 1;
        private ulong _nextModuleId = 1;
        private ulong _nextStackFrameId = 1;

        public ExceptionIdentifierCache(IEnumerable<ExceptionIdentifierCacheCallback> callbacks)
        {
            ArgumentNullException.ThrowIfNull(callbacks);

            _callbacks = callbacks;
        }

        /// <summary>
        /// Gets the identifier of the <see cref="ExceptionIdentifier"/> and
        /// caches its data if it has not been encountered yet.
        /// </summary>
        /// <returns>
        /// An identifier that uniquely identifies the <see cref="ExceptionIdentifier"/> in this cache.
        /// </returns>
        public ulong GetOrAdd(ExceptionIdentifier exceptionId)
        {
            if (_exceptionIds.TryGetValue(exceptionId, out ulong registrationId))
                return registrationId;

            registrationId = Interlocked.Increment(ref _nextRegistrationId);

            ulong actualRegistrationId = _exceptionIds.GetOrAdd(exceptionId, registrationId);

            if (registrationId == actualRegistrationId)
            {
                ExceptionIdentifierData data = new()
                {
                    ExceptionClassId = GetOrAdd(exceptionId.ExceptionType),
                    ThrowingMethodId = AddOrDefault(exceptionId.ThrowingMethod),
                    ILOffset = exceptionId.ILOffset
                };

                InvokeExceptionIdentifierCallbacks(registrationId, data);
            }

            return actualRegistrationId;
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
            ulong methodId = GetId(method);
            if (!_nameCache.FunctionData.ContainsKey(methodId))
            {
                FunctionData data = new(
                    method.Name,
                    AddOrDefault(method.DeclaringType),
                    Convert.ToUInt32(method.MetadataToken),
                    GetOrAdd(method.Module),
                    GetOrAdd(method.GetGenericArguments())
                    );

                if (_nameCache.FunctionData.TryAdd(methodId, data))
                {
                    InvokeFunctionDataCallbacks(methodId, data);
                }
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
            ulong moduleId = GetId(module);
            if (!_nameCache.ModuleData.ContainsKey(moduleId))
            {
                ModuleData data = new(module.Name);

                if (_nameCache.ModuleData.TryAdd(moduleId, data))
                {
                    InvokeModuleDataCallbacks(moduleId, data);
                }
            }
            return moduleId;
        }

        /// <summary>
        /// Gets the identifier of the <see cref="StackFrame"/> instance and
        /// caches its data if it has not be encountered yet.
        /// </summary>
        /// <returns>
        /// An identifier that uniquely identifies the <see cref="StackFrame"/> in this cache.
        /// </returns>
        public ulong GetOrAdd(StackFrame frame)
        {
            StackFrameIdentifier identifier = new(
                AddOrDefault(frame.GetMethod()),
                frame.GetILOffset());

            if (_stackFrameIds.TryGetValue(identifier, out ulong frameId))
                return frameId;

            frameId = Interlocked.Increment(ref _nextStackFrameId);

            ulong actualFrameId = _stackFrameIds.GetOrAdd(identifier, frameId);

            if (actualFrameId == frameId)
            {
                StackFrameData data = new()
                {
                    MethodId = identifier.MethodId,
                    ILOffset = identifier.ILOffset
                };

                InvokeStackFrameDataCallbacks(frameId, data);
            }
            return actualFrameId;
        }

        public ulong[] GetOrAdd(StackFrame[] frames)
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
                ClassData classData = new(
                    typeToken,
                    moduleId,
                    ClassFlags.None,
                    GetOrAdd(type.GetGenericArguments()));

                if (!_nameCache.ClassData.TryAdd(classId, classData))
                    break;

                InvokeClassDataCallbacks(classId, classData);

                ModuleScopedToken key = new(moduleId, typeToken);
                if (!_nameCache.TokenData.ContainsKey(key))
                {
                    uint parentToken = 0;
                    if (null != type.DeclaringType)
                    {
                        parentToken = Convert.ToUInt32(type.DeclaringType.MetadataToken);
                    }

                    TokenData tokenData = new(
                        null == type.DeclaringType ? type.FullName ?? type.Name : type.Name,
                        parentToken);

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

        private static ulong GetId(MethodBase method)
        {
            return Convert.ToUInt64(method.MethodHandle.Value.ToInt64());
        }

        private ulong GetId(Module module)
        {
            return _moduleIds.GetOrAdd(module, _ => _nextModuleId++);
        }

        private static ulong GetId(Type type)
        {
            return Convert.ToUInt64(type.TypeHandle.Value.ToInt64());
        }

        private void InvokeExceptionIdentifierCallbacks(ulong registrationId, ExceptionIdentifierData data)
        {
            foreach (ExceptionIdentifierCacheCallback callback in _callbacks)
            {
                callback.OnExceptionIdentifier(registrationId, data);
            }
        }

        private void InvokeClassDataCallbacks(ulong classId, ClassData data)
        {
            foreach (ExceptionIdentifierCacheCallback callback in _callbacks)
            {
                callback.OnClassData(classId, data);
            }
        }

        private void InvokeFunctionDataCallbacks(ulong functionId, FunctionData data)
        {
            foreach (ExceptionIdentifierCacheCallback callback in _callbacks)
            {
                callback.OnFunctionData(functionId, data);
            }
        }

        private void InvokeModuleDataCallbacks(ulong moduleId, ModuleData data)
        {
            foreach (ExceptionIdentifierCacheCallback callback in _callbacks)
            {
                callback.OnModuleData(moduleId, data);
            }
        }

        private void InvokeStackFrameDataCallbacks(ulong moduleId, StackFrameData data)
        {
            foreach (ExceptionIdentifierCacheCallback callback in _callbacks)
            {
                callback.OnStackFrameData(moduleId, data);
            }
        }

        private void InvokeTokenDataCallbacks(ulong moduleId, uint typeToken, TokenData data)
        {
            foreach (ExceptionIdentifierCacheCallback callback in _callbacks)
            {
                callback.OnTokenData(moduleId, typeToken, data);
            }
        }
    }
}
