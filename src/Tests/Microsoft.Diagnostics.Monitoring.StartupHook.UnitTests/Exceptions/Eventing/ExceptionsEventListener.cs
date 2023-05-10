﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Identification;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Threading;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Eventing
{
    internal sealed class ExceptionsEventListener : EventListener
    {
        public const string NullValue = "{null}";

        public readonly NameCache NameCache = new();
        private readonly Thread _thisThread = Thread.CurrentThread;

        public List<ExceptionInstance> Exceptions { get; } = new();

        public Dictionary<ulong, ExceptionIdentifierData> ExceptionIdentifiers { get; } = new();

        public Dictionary<ulong, StackFrameIdentifier> StackFrameIdentifiers { get; } = new();

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (_thisThread == Thread.CurrentThread)
            {
                ArgumentNullException.ThrowIfNull(eventData.Payload);

                switch (eventData.EventId)
                {
                    case ExceptionEvents.EventIds.ExceptionInstance:
                        Exceptions.Add(
                            new ExceptionInstance(
                                ToUInt64(eventData.Payload[ExceptionEvents.ExceptionInstancePayloads.ExceptionId]),
                                ToString(eventData.Payload[ExceptionEvents.ExceptionInstancePayloads.ExceptionMessage]),
                                ToArray<ulong>(eventData.Payload[ExceptionEvents.ExceptionInstancePayloads.StackFrameIds]),
                                ToType<DateTime>(eventData.Payload[ExceptionEvents.ExceptionInstancePayloads.Timestamp])
                            ));
                        break;
                    case ExceptionEvents.EventIds.ExceptionIdentifier:
                        ExceptionIdentifiers.Add(
                            ToUInt64(eventData.Payload[ExceptionEvents.ExceptionIdentifierPayloads.ExceptionId]),
                            new ExceptionIdentifierData()
                            {
                                ExceptionClassId = ToUInt64(eventData.Payload[ExceptionEvents.ExceptionIdentifierPayloads.ExceptionClassId]),
                                ThrowingMethodId = ToUInt64(eventData.Payload[ExceptionEvents.ExceptionIdentifierPayloads.ThrowingMethodId]),
                                ILOffset = ToInt32(eventData.Payload[ExceptionEvents.ExceptionIdentifierPayloads.ILOffset])
                            });
                        break;
                    case ExceptionEvents.EventIds.ClassDescription:
                        NameCache.ClassData.TryAdd(
                            ToUInt64(eventData.Payload[NameIdentificationEvents.ClassDescPayloads.ClassId]),
                            new ClassData(
                                ToUInt32(eventData.Payload[NameIdentificationEvents.ClassDescPayloads.Token]),
                                ToUInt64(eventData.Payload[NameIdentificationEvents.ClassDescPayloads.ModuleId]),
                                (ClassFlags)ToUInt32(eventData.Payload[NameIdentificationEvents.ClassDescPayloads.Flags]),
                                ToArray<ulong>(eventData.Payload[NameIdentificationEvents.ClassDescPayloads.TypeArgs])));
                        break;
                    case ExceptionEvents.EventIds.FunctionDescription:
                        NameCache.FunctionData.TryAdd(
                            ToUInt64(eventData.Payload[NameIdentificationEvents.FunctionDescPayloads.FunctionId]),
                            new FunctionData(
                                ToString(eventData.Payload[NameIdentificationEvents.FunctionDescPayloads.Name]),
                                ToUInt64(eventData.Payload[NameIdentificationEvents.FunctionDescPayloads.ClassId]),
                                ToUInt32(eventData.Payload[NameIdentificationEvents.FunctionDescPayloads.ClassToken]),
                                ToUInt64(eventData.Payload[NameIdentificationEvents.FunctionDescPayloads.ModuleId]),
                                ToArray<ulong>(eventData.Payload[NameIdentificationEvents.FunctionDescPayloads.TypeArgs])));
                        break;
                    case ExceptionEvents.EventIds.ModuleDescription:
                        NameCache.ModuleData.TryAdd(
                            ToUInt64(eventData.Payload[NameIdentificationEvents.ModuleDescPayloads.ModuleId]),
                            new ModuleData(
                                ToString(eventData.Payload[NameIdentificationEvents.ModuleDescPayloads.Name])));
                        break;
                    case ExceptionEvents.EventIds.StackFrameDescription:
                        StackFrameIdentifiers.TryAdd(
                            ToUInt64(eventData.Payload[ExceptionEvents.StackFrameIdentifierPayloads.StackFrameId]),
                            new StackFrameIdentifier(
                                ToUInt64(eventData.Payload[ExceptionEvents.StackFrameIdentifierPayloads.FunctionId]),
                                ToInt32(eventData.Payload[ExceptionEvents.StackFrameIdentifierPayloads.ILOffset])));
                        break;
                    case ExceptionEvents.EventIds.TokenDescription:
                        NameCache.TokenData.TryAdd(
                            new ModuleScopedToken(
                                ToUInt64(eventData.Payload[NameIdentificationEvents.TokenDescPayloads.ModuleId]),
                                ToUInt32(eventData.Payload[NameIdentificationEvents.TokenDescPayloads.Token])
                                ),
                            new TokenData(
                                ToString(eventData.Payload[NameIdentificationEvents.TokenDescPayloads.Name]),
                                ToUInt32(eventData.Payload[NameIdentificationEvents.TokenDescPayloads.OuterToken])));
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }
        }

        private static int ToInt32(object? value)
        {
            return ToType<int>(value);
        }

        private static uint ToUInt32(object? value)
        {
            return ToType<uint>(value);
        }

        private static ulong ToUInt64(object? value)
        {
            return ToType<ulong>(value);
        }

        private static unsafe T[] ToArray<T>(object? value) where T : unmanaged
        {
            // EventSource doesn't decode non-primitive types very well for EventListeners. In the case of non-byte arrays, it interprets the data
            // as a string and attempts to decode it as a series of chars.
            // Refer to https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Diagnostics/Tracing/EventSource.cs#L146
            return Array.Empty<T>();
        }

        private static string ToString(object? value)
        {
            if (value is null)
            {
                return NullValue;
            }
            return ToType<string>(value);
        }

        private static T ToType<T>(object? value)
        {
            if (value is T typedValue)
            {
                return typedValue;
            }
            throw new InvalidCastException();
        }
    }

    internal sealed class ExceptionInstance
    {
        public ExceptionInstance(ulong exceptionId, string? message, ulong[] frameIds, DateTime timestamp)
        {
            ExceptionId = exceptionId;
            ExceptionMessage = message;
            StackFrameIds = frameIds;
            Timestamp = timestamp;
        }

        public ulong ExceptionId { get; }

        public string? ExceptionMessage { get; }

        public ulong[]? StackFrameIds { get; }

        public DateTime Timestamp { get; }
    }
}
