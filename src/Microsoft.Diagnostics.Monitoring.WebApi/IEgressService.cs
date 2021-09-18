// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal interface IEgressService
    {
        bool CheckProvider(string providerName);

        Task<EgressResult> EgressAsync(
            string providerName,
            Func<CancellationToken, Task<Stream>> action,
            string fileName,
            string contentType,
            IProcessInfo source,
            CancellationToken token);

        Task<EgressResult> EgressAsync(
            string providerName,
            Func<Stream, CancellationToken, Task> action,
            string fileName,
            string contentType,
            IProcessInfo source,
            CancellationToken token);
    }
}
