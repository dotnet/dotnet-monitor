// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi.Stacks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json.Serialization;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Models
{
    [DebuggerDisplay("{ModuleName,nq}!{TypeName,nq}.{MethodName,nq}")]
    public class CallStackFrame
    {
        [JsonPropertyName("methodName")]
        public string MethodNameWithGenericArgTypes
        {
            get
            {
                StringBuilder builder = new(MethodName);
                NameFormatter.BuildGenericArgTypes(builder, FullGenericArgTypes);
                return builder.ToString();
            }
            // Only intended for test code.
            set
            {
                MethodName = NameFormatter.RemoveGenericArgTypes(value, out string[] genericArgTypes);
                FullGenericArgTypes = genericArgTypes;
            }
        }

        [JsonIgnore]
        internal string MethodName { get; set; } = string.Empty;

        [JsonPropertyName("methodToken")]
        public uint MethodToken { get; set; }

        [JsonPropertyName("parameterTypes")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IList<string>? FullParameterTypes { get; set; }

        [JsonIgnore]
        internal IList<string>? SimpleParameterTypes { get; set; }

        [JsonPropertyName("typeName")]
        public string TypeName { get; set; } = string.Empty;

        [JsonPropertyName("moduleName")]
        public string ModuleName { get; set; } = string.Empty;

        [JsonPropertyName("moduleVersionId")]
        public Guid ModuleVersionId { get; set; }

        [JsonPropertyName("hidden")]
        public bool Hidden { get; set; }

        [JsonIgnore]
        internal IList<string> SimpleGenericArgTypes { get; set; } = new List<string>();

        [JsonIgnore]
        internal IList<string> FullGenericArgTypes { get; set; } = new List<string>();

        //TODO Bring this back once we have a relative il offset value.
        //[JsonPropertyName("offset")]
        //public ulong Offset { get; set; }
    }

    public class CallStack
    {
        [JsonPropertyName("threadId")]
        public uint ThreadId { get; set; }

        [JsonPropertyName("threadName")]
        public string ThreadName { get; set; } = string.Empty;

        [JsonPropertyName("frames")]
        public IList<CallStackFrame> Frames { get; set; } = new List<CallStackFrame>();
    }

    public class CallStackResult
    {
        [JsonPropertyName("stacks")]
        public IList<CallStack> Stacks { get; set; } = new List<CallStack>();
    }
}
