// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    public sealed class PrefixedOutputHelper : ITestOutputHelper
    {
        private readonly ITestOutputHelper _outputHelper;
        private readonly string _prefix;

        public PrefixedOutputHelper(ITestOutputHelper outputHelper, string prefix)
        {
            _outputHelper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));
            _prefix = prefix ?? throw new ArgumentNullException(nameof(prefix));
        }

        public void WriteLine(string message)
        {
            _outputHelper.WriteLine($"{_prefix}{message}");
        }

        public void WriteLine(string format, params object[] args)
        {
            _outputHelper.WriteLine($"{_prefix}{format}", args);
        }
    }
}
