// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Models
{
    public class SpeedscopeResult
    {
        [JsonPropertyName("exporter")]
        public string? Exporter { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("activeProfileIndex")]
        public int ActiveProfileIndex { get; set; }

        [JsonPropertyName("$schema")]
        public string Schema { get; set; } = "https://www.speedscope.app/file-format-schema.json";

        [JsonPropertyName("profiles")]
        public List<Profile>? Profiles { get; set; }

        [JsonPropertyName("shared")]
        public SharedFrames? Shared { get; set; }
    }

    public class SharedFrames
    {
        [JsonPropertyName("frames")]
        public List<SharedFrame>? Frames { get; set; }
    }

    public class SharedFrame
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("col")]
        public int? Column { get; set; }

        [JsonPropertyName("line")]
        public int? Line { get; set; }

        [JsonPropertyName("file")]
        public string? File { get; set; }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ProfileType
    {
        evented,
        sampled
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum UnitType
    {
        none,
        nanoseconds,
        microseconds,
        milliseconds,
        seconds,
        bytes,
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ProfileEventType
    {
        O,
        C
    }

    public class ProfileEvent
    {
        [JsonPropertyName("type")]
        public ProfileEventType Type { get; set; }

        [JsonPropertyName("frame")]
        public int Frame { get; set; }

        [JsonPropertyName("at")]
        public double At { get; set; }
    }

    public class Profile
    {
        [JsonPropertyName("type")]
        public ProfileType Type { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("unit")]
        public UnitType Unit { get; set; }

        [JsonPropertyName("startValue")]
        public double StartValue { get; set; }

        [JsonPropertyName("endValue")]
        public double EndValue { get; set; }

        [JsonPropertyName("events")]
        public List<ProfileEvent>? Events { get; set; }
    }
}
