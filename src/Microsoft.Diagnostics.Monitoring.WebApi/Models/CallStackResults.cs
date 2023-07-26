// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Models
{
    public class CallStackFrame
    {
        [JsonPropertyName("methodName")]
        public string MethodName { get; set; }

        [JsonPropertyName("className")]
        public string ClassName { get; set; }

        [JsonPropertyName("moduleName")]
        public string ModuleName { get; set; }

        [JsonIgnore]
        internal IList<string> GenericParameters { get; set; } = new List<string>();

        [JsonIgnore]
        internal IList<string> FriendlyGenericParameters { get; set; } = new List<string>();

        [JsonIgnore]
        internal IList<string> ParameterTypes { get; set; } = new List<string>();

        [JsonIgnore]
        internal IList<string> FriendlyParameterTypes { get; set; } = new List<string>();
        //TODO Bring this back once we have a relative il offset value.
        //[JsonPropertyName("offset")]
        //public ulong Offset { get; set; }
    }

    public class CallStack
    {
        [JsonPropertyName("threadId")]
        public uint ThreadId { get; set; }

        [JsonPropertyName("threadName")]
        public string ThreadName { get; set; }

        [JsonPropertyName("frames")]
        public IList<CallStackFrame> Frames { get; set; } = new List<CallStackFrame>();
    }

    public class CallStackResult
    {
        [JsonPropertyName("stacks")]
        public IList<CallStack> Stacks { get; set; } = new List<CallStack>();
    }
}
