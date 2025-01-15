// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Stacks
{
    internal sealed class SpeedscopeStacksFormatter : StacksFormatter
    {
        private const double FixedStart = 0.0;
        private const double FixedEnd = 1.0;
        private static readonly string Exporter = FormattableString.Invariant($"dotnetmonitor@{Assembly.GetExecutingAssembly().GetInformationalVersionString()}");
        private const string Name = "speedscope.json";
        private static readonly ProfileEvent NativeProfileEvent = new ProfileEvent { At = FixedStart, Frame = 0, Type = ProfileEventType.O };
        private static readonly ProfileEvent UnknownProfileEvent = new ProfileEvent { At = FixedStart, Frame = 1, Type = ProfileEventType.O };

        public SpeedscopeStacksFormatter(Stream outputStream) : base(outputStream)
        {
        }

        public override async Task FormatStack(CallStackResult stackResult, CancellationToken token)
        {
            // Speedscope contains a shared set of frames and each callstack references an index
            // into the shared frame pool.
            // We map each FunctionId to a corresponding index.
            // Index 0 is reserved for Native frames
            // Index 1 has no corresponding FunctionId and is reserved for Unknown.
            var functionToSharedFrameMap = new Dictionary<ulong, int>()
            {
                {0, 0}
            };

            var speedscopeResult = new Models.SpeedscopeResult();

            speedscopeResult.ActiveProfileIndex = 0;
            speedscopeResult.Profiles = new List<Models.Profile>(stackResult.Stacks.Count);
            speedscopeResult.Exporter = Exporter;
            speedscopeResult.Name = Name;
            speedscopeResult.Shared = new Models.SharedFrames
            {
                Frames = new List<Models.SharedFrame>()
                {
                    new Models.SharedFrame{ Name = NativeFrame },
                    new Models.SharedFrame{ Name = NameFormatter.UnknownClass }
                }
            };

            NameCache cache = stackResult.NameCache;
            var builder = new StringBuilder();

            foreach (CallStack stack in stackResult.Stacks)
            {
                Models.Profile profile = new Profile()
                {
                    Events = new List<ProfileEvent>(),
                    StartValue = FixedStart,
                    EndValue = FixedEnd,
                    Unit = UnitType.none,
                    Name = FormatThreadName(stack.ThreadId, stack.ThreadName)
                };

                speedscopeResult.Profiles.Add(profile);

                foreach (CallStackFrame frame in stack.Frames)
                {
                    if (frame.FunctionId == 0)
                    {
                        profile.Events.Add(NativeProfileEvent);

                    }
                    else if (cache.FunctionData.TryGetValue(frame.FunctionId, out FunctionData? functionData))
                    {
                        if (StackUtilities.ShouldHideFunctionFromStackTrace(cache, functionData))
                        {
                            continue;
                        }

                        if (!functionToSharedFrameMap.TryGetValue(frame.FunctionId, out int mapping))
                        {
                            // Note this may imply some duplicate frames because we use FunctionId as a unique identifier for a frame,
                            // but Speedscope uses the name.

                            builder.Clear();
                            builder.Append(NameFormatter.GetModuleName(cache, functionData.ModuleId));
                            builder.Append(ModuleSeparator);
                            NameFormatter.BuildTypeName(builder, cache, functionData);
                            builder.Append(ClassSeparator);
                            builder.Append(functionData.Name);
                            NameFormatter.BuildGenericTypeNames(builder, cache, functionData.TypeArgs);

                            speedscopeResult.Shared.Frames.Add(new SharedFrame { Name = builder.ToString() });
                            mapping = speedscopeResult.Shared.Frames.Count - 1;
                            functionToSharedFrameMap.Add(frame.FunctionId, mapping);
                        }

                        var profileEvent = new ProfileEvent
                        {
                            At = FixedStart,
                            Frame = mapping,
                            Type = ProfileEventType.O,
                        };

                        profile.Events.Add(profileEvent);

                    }
                    else
                    {
                        profile.Events.Add(UnknownProfileEvent);
                    }
                }

                // Speedscope requires a close event for each open event.
                for (int i = profile.Events.Count - 1; i >= 0; i--)
                {
                    profile.Events.Add(new ProfileEvent { At = FixedEnd, Frame = profile.Events[i].Frame, Type = ProfileEventType.C });
                }
            }

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
            await JsonSerializer.SerializeAsync(OutputStream, speedscopeResult, options, cancellationToken: token);
        }
    }
}
