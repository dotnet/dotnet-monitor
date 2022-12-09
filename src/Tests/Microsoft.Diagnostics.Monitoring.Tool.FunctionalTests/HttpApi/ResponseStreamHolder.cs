// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.HttpApi
{
    /// <summary>
    /// Holds a <see cref="System.IO.Stream"/> from a <see cref="HttpResponseMessage"/> to
    /// ensure that the message and stream are disposed together.
    /// </summary>
    internal class ResponseStreamHolder : IDisposable
    {
        private readonly HttpResponseMessage _response;

        public Stream Stream { get; private set; }

        private ResponseStreamHolder(HttpResponseMessage response)
        {
            _response = response ?? throw new ArgumentNullException(nameof(response));
        }

        public void Dispose()
        {
            // The response disposes the stream when disposed.
            _response.Dispose();
        }

        public static async Task<ResponseStreamHolder> CreateAsync(DisposableBox<HttpResponseMessage> responseBox)
        {
            using DisposableBox<ResponseStreamHolder> holderBox = new(new(responseBox.Release()));
            holderBox.Value.Stream = await holderBox.Value._response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            return holderBox.Release();
        }
    }
}
