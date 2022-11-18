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
        private readonly bool _stdout;

        public ConsoleOutputHelper(bool stdout = true)
        {
            _stdout = stdout;
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
            return (_stdout) ? Console.Out : Console.Error;
        }
    }
}
