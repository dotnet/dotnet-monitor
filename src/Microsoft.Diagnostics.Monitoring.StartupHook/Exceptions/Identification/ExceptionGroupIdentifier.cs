// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Reflection;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Identification
{
    /// <summary>
    /// Class representing a type of exception thrown from the specific location
    /// in code. This class may be used as an identifier for determining whether
    /// exception usages are equivalent (same exception type thrown from the same location).
    /// </summary>
    internal sealed record class ExceptionGroupIdentifier
    {
        public ExceptionGroupIdentifier(Exception ex)
        {
            ArgumentNullException.ThrowIfNull(ex);

            ExceptionType = ex.GetType();

            StackTrace stackTrace = new(ex, fNeedFileInfo: false);
            SetThrowingFrame(stackTrace.GetFrames());
        }

        public ExceptionGroupIdentifier(Exception ex, ReadOnlySpan<StackFrame> stackFrames)
        {
            ArgumentNullException.ThrowIfNull(ex);

            ExceptionType = ex.GetType();

            SetThrowingFrame(stackFrames);
        }

        private void SetThrowingFrame(ReadOnlySpan<StackFrame> stackFrames)
        {
            foreach (StackFrame stackFrame in stackFrames)
            {
                ThrowingMethod = stackFrame.GetMethod();
                if (null != ThrowingMethod)
                {
                    ILOffset = stackFrame.GetILOffset();
                    break;
                }
            }
        }

        public Type ExceptionType { get; }

        public MethodBase? ThrowingMethod { get; private set; }

        public int ILOffset { get; private set; }
    }
}
