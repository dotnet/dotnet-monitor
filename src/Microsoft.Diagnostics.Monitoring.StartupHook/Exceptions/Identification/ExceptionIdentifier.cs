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
    internal sealed record class ExceptionIdentifier
    {
        public ExceptionIdentifier(Exception ex)
        {
            ArgumentNullException.ThrowIfNull(ex);

            ExceptionType = ex.GetType();

            StackTrace stackTrace = new(ex, fNeedFileInfo: false);
            foreach (StackFrame stackFrame in stackTrace.GetFrames())
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

        public MethodBase? ThrowingMethod { get; }

        public int ILOffset { get; }
    }
}
