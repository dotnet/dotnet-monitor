// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotnetMonitor.ParameterCapture
{
    /// <summary>
    /// Special namespace and class used for Parameter Capture logging.
    /// This is to prevent log filtering that normally occurs for Microsoft categories.
    /// We can also use this to classify different types of logs, such as user vs. library code.
    /// </summary>
    internal sealed class UserCode
    {
    }

    internal sealed class SystemCode
    {
    }
}



