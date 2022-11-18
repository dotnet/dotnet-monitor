// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    public sealed class ConsoleOutputHelper : ITestOutputHelper
    {
        public enum OutputStream
        {
            Stdout,
            Stderr
        }

        private readonly OutputStream _outputStream;

        public ConsoleOutputHelper(OutputStream outputStream = OutputStream.Stdout)
        {
            _outputStream = outputStream;
        }

        public void WriteLine(string message)
        {
            GetOutputWriter().WriteLine(message);
        }

        public void WriteLine(string format, params object[] args)
        {
            GetOutputWriter().WriteLine(format, args);
        }

        private TextWriter GetOutputWriter()
        {
            return (_outputStream == OutputStream.Stdout) ? Console.Out : Console.Error;
        }
    }
}
