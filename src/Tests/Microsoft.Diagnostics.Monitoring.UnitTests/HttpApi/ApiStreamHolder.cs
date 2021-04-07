// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.UnitTests.HttpApi
{
    internal class ApiStreamHolder : IDisposable
    {
        private readonly HttpResponseMessage _response;

        public Stream Stream { get; private set; }

        private ApiStreamHolder(HttpResponseMessage response)
        {
            _response = response ?? throw new ArgumentNullException(nameof(response));
        }

        public void Dispose()
        {
            _response.Dispose();
            Stream?.Dispose();
        }

        public static async Task<ApiStreamHolder> CreateAsync(HttpResponseMessage response)
        {
            ApiStreamHolder holder = new(response);
            holder.Stream = await response.Content.ReadAsStreamAsync();
            return holder;
        }
    }
}
