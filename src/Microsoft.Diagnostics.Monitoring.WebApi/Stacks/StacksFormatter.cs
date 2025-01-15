// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Stacks
{
    internal abstract class StacksFormatter
    {
        public const string UnknownFunction = "UnknownFunction";

        public const string NativeFrame = "[NativeFrame]";

        protected const char ModuleSeparator = '!';
        protected const char ClassSeparator = '.';

        protected Stream OutputStream { get; }

        public StacksFormatter(Stream outputStream)
        {
            OutputStream = outputStream;
        }

        public abstract Task FormatStack(CallStackResult stackResult, CancellationToken token);

        protected static string FormatThreadName(uint threadId, string? threadName)
        {
            const string Separator = " ";

            string fullThreadName = string.Format(CultureInfo.CurrentCulture, Strings.CallstackThreadHeader, threadId);

            if (!string.IsNullOrEmpty(threadName))
            {
                return string.Concat(fullThreadName, Separator, threadName);
            }
            return fullThreadName;
        }
    }
}
