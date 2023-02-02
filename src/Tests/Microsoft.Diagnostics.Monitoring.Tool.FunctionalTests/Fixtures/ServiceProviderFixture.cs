// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Fixtures
{
    /// <summary>
    /// Provides common services to tests.
    /// </summary>
    public class ServiceProviderFixture : IDisposable
    {
        public const string HttpClientName_DefaultCredentials = "DefaultCredentials";
        public const string HttpClientName_NoRedirect = "NoRedirect";

        public ServiceProviderFixture()
        {
            ServiceCollection services = new();
            services.AddHttpClient();
            services.AddHttpClient(HttpClientName_DefaultCredentials)
                .ConfigurePrimaryHttpMessageHandler(() =>
                {
                    HttpClientHandler handler = new HttpClientHandler();
                    handler.UseDefaultCredentials = true;
                    return handler;
                });

            services.AddHttpClient(HttpClientName_NoRedirect)
                .ConfigurePrimaryHttpMessageHandler(() =>
                {
                    HttpClientHandler handler = new HttpClientHandler();
                    handler.AllowAutoRedirect = false;
                    return handler;
                });

            ServiceProvider = services.BuildServiceProvider();
        }

        public ServiceProvider ServiceProvider { get; }

        public void Dispose()
        {
            ServiceProvider.Dispose();
        }
    }
}
