// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Stacks
{
    internal sealed class TextStacksFormatter : StacksFormatter
    {
        private const string Indent = "  ";

        public TextStacksFormatter(Stream outputStream) : base(outputStream)
        {
        }

        public override async Task FormatStack(CallStackResult stackResult, CancellationToken token)
        {
            await using StreamWriter writer = new StreamWriter(this.OutputStream, Encoding.UTF8, leaveOpen: true);
            var builder = new StringBuilder();
            foreach (var stack in stackResult.Stacks)
            {
                token.ThrowIfCancellationRequested();

                await writer.WriteLineAsync(FormatThreadName(stack.ThreadId, stack.ThreadName));
                foreach (var frame in stack.Frames)
                {
                    builder.Clear();
                    builder.Append(Indent);
                    if (BuildFrame(builder, stackResult.NameCache, frame))
                    {
                        await writer.WriteLineAsync(builder, token);
                    }
                }
                await writer.WriteLineAsync();
            }

#if NET8_0_OR_GREATER
            await writer.FlushAsync(token);
#else
            await writer.FlushAsync();
#endif
        }

        /// <returns>True if the frame should be included in the stack trace.</returns>
        private static bool BuildFrame(StringBuilder builder, NameCache cache, CallStackFrame frame)
        {
            if (frame.FunctionId == 0)
            {
                builder.Append(NativeFrame);
            }
            else if (cache.FunctionData.TryGetValue(frame.FunctionId, out FunctionData? functionData))
            {
                if (StackUtilities.ShouldHideFunctionFromStackTrace(cache, functionData))
                {
                    return false;
                }

                builder.Append(NameFormatter.GetModuleName(cache, functionData.ModuleId));
                builder.Append(ModuleSeparator);
                NameFormatter.BuildTypeName(builder, cache, functionData);
                builder.Append(ClassSeparator);
                builder.Append(functionData.Name);
                NameFormatter.BuildGenericTypeNames(builder, cache, functionData.TypeArgs, NameFormatter.TypeFormat.Simple);
            }
            else
            {
                builder.Append(UnknownFunction);
            }

            return true;
        }
    }
}
