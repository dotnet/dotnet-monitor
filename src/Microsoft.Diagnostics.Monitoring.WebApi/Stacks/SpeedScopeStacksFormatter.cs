// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Diagnostics.Tracing.Parsers.FrameworkEventSource;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Stacks
{
    internal sealed class SpeedScopeStacksFormatter : StacksFormatter
    {
        private const double FixedStart = 0.0;
        private const double FixedEnd = 1.0;
        private static readonly string Exporter = FormattableString.Invariant($"dotnetmonitor@{Assembly.GetExecutingAssembly().GetInformationalVersionString()}");
        private const string Name = "speedscope.json";

        public SpeedScopeStacksFormatter(Stream outputStream) : base(outputStream)
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

            var speedScopeResult = new Models.SpeedScopeResult();

            speedScopeResult.ActiveProfileIndex = 0;
            speedScopeResult.Profiles = new List<Models.Profile>();
            speedScopeResult.Exporter = Exporter;
            speedScopeResult.Name = Name;
            speedScopeResult.Shared = new Models.SharedFrames
            {
                Frames = new List<Models.SharedFrame>()
                {
                    new Models.SharedFrame{ Name = NativeFrame },
                    new Models.SharedFrame{ Name = UnknownClass }
                }
            };

            NameCache cache = stackResult.NameCache;

            foreach (CallStack stack in stackResult.Stacks)
            {
                Models.Profile profile = new Profile()
                {
                    Events = new List<ProfileEvent>(),
                    StartValue = FixedStart,
                    EndValue = FixedEnd,
                    Unit = UnitType.none,
                    Name = string.Format(CultureInfo.CurrentCulture, Strings.CallstackThreadHeader, stack.ThreadId),
                };

                speedScopeResult.Profiles.Add(profile);

                foreach (CallStackFrame frame in stack.Frames)
                {
                    if (frame.FunctionId == 0)
                    {
                        var profileEvent = new ProfileEvent { At = FixedStart, Frame = 0, Type = ProfileEventType.O };

                    }
                    else if (cache.FunctionData.TryGetValue(frame.FunctionId, out FunctionData functionData))
                    {
                        var builder = new StringBuilder();
                        builder.Append(GetModuleName(cache, functionData.ModuleId));
                        builder.Append(ModuleSeparator);
                        BuildClassName(builder, cache, functionData);
                        builder.Append(ClassSeparator);
                        builder.Append(functionData.Name);
                        BuildGenericParameters(builder, cache, functionData.TypeArgs);

                        if (!functionToSharedFrameMap.TryGetValue(frame.FunctionId, out int mapping))
                        {
                            speedScopeResult.Shared.Frames.Add(new SharedFrame { Name = builder.ToString() });
                            mapping = speedScopeResult.Shared.Frames.Count - 1;
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
                        profile.Events.Add(new ProfileEvent { At = FixedStart, Frame = 1, Type= ProfileEventType.O });
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
#if NET6_0_OR_GREATER
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
#endif
            };
            await JsonSerializer.SerializeAsync(OutputStream, speedScopeResult, options, cancellationToken: token);
        }
    }
}
