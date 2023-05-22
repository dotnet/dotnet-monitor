// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Stacks
{
    internal abstract class StacksFormatter
    {
        internal const string UnknownFunction = "UnknownFunction";

        internal const string NativeFrame = "[NativeFrame]";

        protected const char ModuleSeparator = '!';
        protected const char ClassSeparator = '.';

        protected Stream OutputStream { get; }

        public StacksFormatter(Stream outputStream)
        {
            OutputStream = outputStream;
        }

        public abstract Task FormatStack(CallStackResult stackResult, CancellationToken token);

        protected static string FormatThreadName(uint threadId, string threadName)
        {
            const string Separator = " ";

            string fullThreadName = string.Format(CultureInfo.CurrentCulture, Strings.CallstackThreadHeader, threadId);

            if (!string.IsNullOrEmpty(threadName))
            {
                return string.Concat(fullThreadName, Separator, threadName);
            }
            return fullThreadName;
        }

        internal static Models.CallStackFrame CreateFrameModel(CallStackFrame frame, NameCache cache)
        {
            var builder = new StringBuilder();

            Models.CallStackFrame frameModel = new Models.CallStackFrame()
            {
                ClassName = NameFormatter.UnknownClass,
                MethodName = UnknownFunction,
                //TODO Bring this back once we have a useful offset value
                //Offset = frame.Offset,
                ModuleName = NameFormatter.UnknownModule
            };
            if (frame.FunctionId == 0)
            {
                frameModel.MethodName = NativeFrame;
                frameModel.ModuleName = NativeFrame;
                frameModel.ClassName = NativeFrame;
            }
            else if (cache.FunctionData.TryGetValue(frame.FunctionId, out FunctionData functionData))
            {
                frameModel.ModuleName = NameFormatter.GetModuleName(cache, functionData.ModuleId);
                frameModel.MethodName = functionData.Name;

                builder.Clear();
                NameFormatter.BuildClassName(builder, cache, functionData);
                frameModel.ClassName = builder.ToString();

                if (functionData.TypeArgs.Length > 0)
                {
                    builder.Clear();
                    builder.Append(functionData.Name);
                    NameFormatter.BuildGenericParameters(builder, cache, functionData.TypeArgs);
                    frameModel.MethodName = builder.ToString();
                }
            }

            return frameModel;
        }
    }
}
