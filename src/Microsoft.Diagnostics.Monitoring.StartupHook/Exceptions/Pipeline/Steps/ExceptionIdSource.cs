// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions.Pipeline.Steps
{
    internal sealed class ExceptionIdSource
    {
        private readonly object _exceptionIdKey = new object();

        private ulong _nextExceptionId = 1;

        public ulong GetId(Exception? exception)
        {
            if (null == exception)
                return 0;

            if (TryGetExceptionId(exception, out ulong exceptionId))
                return exceptionId;

            lock (exception.Data)
            {
                if (TryGetExceptionId(exception, out exceptionId))
                    return exceptionId;

                exceptionId = Interlocked.Increment(ref _nextExceptionId);

                exception.Data[_exceptionIdKey] = exceptionId;
            }

            return exceptionId;

            bool TryGetExceptionId(Exception exception, out ulong exceptionId)
            {
                if (exception.Data.Contains(_exceptionIdKey))
                {
                    // The ExceptionIdKey data should only ever have a ulong
                    if (exception.Data[_exceptionIdKey] is ulong exceptionIdCandidate)
                    {
                        exceptionId = exceptionIdCandidate;
                        return true;
                    }
                }

                exceptionId = default;
                return false;
            }
        }
    }
}
