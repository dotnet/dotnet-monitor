// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;
using Swashbuckle.AspNetCore.Swagger;
using System.IO;

namespace Microsoft.Diagnostics.Tools.Monitor.Swagger
{
    internal static class SwaggerProviderExtensions
    {
        public static void WriteTo(this ISwaggerProvider provider, Stream stream)
        {
            using StreamWriter writer = new(stream);

            provider.WriteTo(writer);
        }

        public static void WriteTo(this ISwaggerProvider provider, TextWriter writer)
        {
            OpenApiDocument document = provider.GetSwagger("v1");

            document.SerializeAsV3(new OpenApiJsonWriter(writer));
        }
    }
}
