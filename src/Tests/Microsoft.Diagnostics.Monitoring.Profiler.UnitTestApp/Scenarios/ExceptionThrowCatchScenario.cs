// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.CommandLine;

namespace Microsoft.Diagnostics.Monitoring.Profiler.UnitTestApp.Scenarios
{
    /// <summary>
    /// Async waits until it receives the Continue command.
    /// </summary>
    internal static class ExceptionThrowCatchScenario
    {
        public static Command Command()
        {
            Command command = new("ExceptionThrowCatch");
            command.SetAction(Execute);
            return command;
        }

        public static void Execute(ParseResult result)
        {
            ThrowCatch();
            ThrowCatchDeep();
            ThrowCatchRethrowCatch();
        }

        private static void ThrowCatch()
        {
            try
            {
                throw new InvalidOperationException();
            }
            catch
            {
            }
        }

        private static void ThrowCatchDeep()
        {
            try
            {
                ThrowCatchDeepInner();
            }
            catch
            {
            }
        }

        private static void ThrowCatchDeepInner()
        {
            Throw();
        }

        private static void ThrowCatchRethrowCatch()
        {
            try
            {
                ThrowCatchRethrowCatchInner();
            }
            catch
            {
            }
        }

        private static void ThrowCatchRethrowCatchInner()
        {
            try
            {
                Throw();
            }
            catch
            {
                throw;
            }
        }

        private static void Throw()
        {
            throw new InvalidOperationException();
        }
    }
}
