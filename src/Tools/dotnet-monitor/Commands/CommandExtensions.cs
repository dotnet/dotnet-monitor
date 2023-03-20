// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;

#if UNITTEST
namespace Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios
#else
namespace Microsoft.Diagnostics.Tools.Monitor.Commands
#endif
{
    internal static class CommandExtensions
    {
        // Due to https://github.com/dotnet/command-line-api/pull/2095, returning an exit code is "pay-for-play". A custom CliAction
        // must be implemented in order to actually observe the exit code. Otherwise, SetAction will happily accept a callback that
        // looks like "Func<InvocationContext, CancellationToken, Task<int>>", await it, but not observe the result since the signature
        // of the action parameter is "Func<InvocationContext, CancellationToken, Task>".
        public static void SetActionWithExitCode(this Command command, Func<InvocationContext, CancellationToken, Task<int>> action)
        {
            command.Action = new CliActionWithExitCode(action);
        }

        private sealed class CliActionWithExitCode : CliAction
        {
            private Func<InvocationContext, CancellationToken, Task<int>> _action;

            public CliActionWithExitCode(Func<InvocationContext, CancellationToken, Task<int>> action)
            {
                ArgumentNullException.ThrowIfNull(action);

                _action = action;
            }

            public override int Invoke(InvocationContext context)
            {
                throw new NotSupportedException();
            }

            public override Task<int> InvokeAsync(InvocationContext context, CancellationToken cancellationToken = default)
            {
                return _action(context, cancellationToken);
            }
        }
    }
}
