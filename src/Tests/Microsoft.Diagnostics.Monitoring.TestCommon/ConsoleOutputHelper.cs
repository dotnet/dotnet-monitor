// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    public sealed class ConsoleOutputHelper : ITestOutputHelper
    {
        // This stores either stdout/stderr, no need to dispose.
        private readonly TextWriter _outputWriter;

        public ConsoleOutputHelper(bool stdout = true)
        {
            _outputWriter = (stdout) ? Console.Out : Console.Error;
        }

        public void WriteLine(string message)
        {
            _outputWriter.WriteLine(message);
        }

        public void WriteLine(string format, params object[] args)
        {
            _outputWriter.WriteLine(format, args);
        }
    }
}
