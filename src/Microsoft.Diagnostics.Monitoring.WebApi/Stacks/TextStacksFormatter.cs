// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Stacks
{
    internal sealed class TextStacksFormatter : StacksFormatter
    {
        private const char ModuleSeparator = '!';
        private const char ClassSeparator = '.';
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

                await writer.WriteLineAsync(string.Format(CultureInfo.CurrentCulture, Strings.CallstackThreadHeader, stack.ThreadId));
                foreach (var frame in stack.Frames)
                {
                    builder.Clear();
                    builder.Append(Indent);
                    BuildFrame(builder, stackResult.NameCache, frame);
                    await writer.WriteLineAsync(builder, token);
                }
                await writer.WriteLineAsync();
            }
        }

        private void BuildFrame(StringBuilder builder, NameCache cache, CallStackFrame frame)
        {
            if (frame.FunctionId == 0)
            {
                builder.Append(NativeFrame);
            }
            else if (cache.FunctionData.TryGetValue(frame.FunctionId, out FunctionData functionData))
            {
                builder.Append(base.GetModuleName(cache, functionData.ModuleId));
                builder.Append(ModuleSeparator);
                if (functionData.ParentClass != 0)
                {
                    BuildClassName(builder, cache, functionData.ParentClass);
                }
                else
                {
                    BuildClassName(builder, cache, functionData.ModuleId, functionData.ParentToken);
                }
                builder.Append(ClassSeparator);
                builder.Append(functionData.Name);
                BuildGenericParameters(builder, cache, functionData.TypeArgs);
            }
            else
            {
                builder.Append(UnknownFunction);
            }
        }
    }
}
