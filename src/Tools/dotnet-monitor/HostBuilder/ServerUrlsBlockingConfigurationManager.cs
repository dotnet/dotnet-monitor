// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Tools.Monitor
{
    /// <summary>
    /// A managing class that contains the state that determines whether the
    /// ServerUrlsBlockingConfigurationProvider should block reading the Urls
    /// option from the providers configured at a lower precedence.
    /// </summary>
    internal sealed class ServerUrlsBlockingConfigurationManager
    {
        public bool IsBlocking { get; set; }
    }
}
