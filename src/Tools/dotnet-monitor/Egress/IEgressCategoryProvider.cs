// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress
{
    internal interface IEgressCategoryProvider
    {
        /// <summary>
        /// The configuration section for egress provider.
        /// </summary>
        IConfiguration Configuration { get; }
    }
}
