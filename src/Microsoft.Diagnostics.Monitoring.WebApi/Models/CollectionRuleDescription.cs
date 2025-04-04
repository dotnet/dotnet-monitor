﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using System.ComponentModel;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Models
{
    public record class CollectionRuleDescription
    {
        /// <summary>
        /// Indicates what state the collection rule is in for the process.
        /// </summary>
        [JsonPropertyName("state")]
        public CollectionRuleState State { get; set; }

        [Description("Human-readable explanation for the current state of the collection rule.")]
        [JsonPropertyName("stateReason")]
        public string? StateReason { get; set; }
    }
}
