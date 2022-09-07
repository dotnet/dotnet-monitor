// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Models
{
    public class StackFrame
    {
        [JsonPropertyName("methodName")]
        public string MethodName { get; set; }

        [JsonPropertyName("className")]
        public string ClassName { get; set; }

        [JsonPropertyName("moduleName")]
        public string ModuleName { get; set; }

        [JsonPropertyName("offset")]
        public ulong Offset { get; set; }
    }

    public class Stack
    {
        [JsonPropertyName("threadId")]
        public uint ThreadId { get; set; }

        [JsonPropertyName("frames")]
        public IList<StackFrame> Frames { get;set;} = new List<StackFrame>();
    }

    public class StackResult
    {
        [JsonPropertyName("stacks")]
        public IList<Stack> Stacks { get; set; } = new List<Stack>();
    }
}
