// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace Microsoft.Diagnostics.Monitoring.ConfigurationSchema
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                throw new InvalidOperationException("Missing output path");
            }

            var generator = new SchemaGenerator();
            string result = generator.GenerateSchema();

            File.WriteAllText(args[0], result);
        }
    }
}
