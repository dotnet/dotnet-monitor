// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.Tools.Monitor.Auth
{
    internal sealed class AuthenticationStartupLoggerWrapper : IStartupLogger
    {
        private readonly Action _logAction;

        public AuthenticationStartupLoggerWrapper(Action log)
        {
            _logAction = log;
        }

        public void Log()
        {
            _logAction();
        }
    }
}
