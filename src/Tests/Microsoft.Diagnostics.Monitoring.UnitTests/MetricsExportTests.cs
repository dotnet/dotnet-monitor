﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.UnitTests
{
    public class MetricsExportTests
    {
        [Fact]
        public void TestPrometheusNormalization()
        {
            string metric = Normalize("Test-Provider", "Test-Metric");
            Assert.Equal("testprovider_Test_Metric_bytes", metric);

            metric = Normalize("!@#", "#@#");
            //<provider>_<metric>_<unit>
            //provider becomes '_'
            //metric becomes '___'
            //unit defaults to bytes
            Assert.Equal("______bytes", metric);

            metric = Normalize("Asp-Net-Provider", "Requests!Received", "0$customs");
            Assert.Equal("aspnetprovider_Requests_Received___customs", metric);

            metric = Normalize("a", "b", unit: null);
            Assert.Equal("a_b", metric);

            metric = Normalize("UnicodeάήΰLetter", "Unicode\u0befDigit", unit: null);
            Assert.Equal("unicodeletter_Unicode_Digit", metric);
        }

        private static string Normalize(string provider, string name, string unit = "b")
        {
            return PrometheusDataModel.Normalize(provider, name, unit, 0.0, out _);
        }
    }
}
