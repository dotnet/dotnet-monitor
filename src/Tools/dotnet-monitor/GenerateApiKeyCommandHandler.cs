// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    /// <summary>
    /// Used to generate Api Key for authentication. The first output is
    /// part of the Authorization header, and is the Base64 encoded key.
    /// The second output is a hex encoded string of the hash of the secret.
    /// </summary>
    internal sealed class GenerateApiKeyCommandHandler
    {
        public Task<int> GenerateApiKey(CancellationToken token, IConsole console)
        {
            GeneratedApiKey newKey = GeneratedApiKey.Create();

            console.Out.WriteLine(FormattableString.Invariant($"Authorization: {Monitoring.RestServer.AuthConstants.ApiKeySchema} {newKey.MonitorApiKey}"));
            console.Out.WriteLine(FormattableString.Invariant($"ApiKeyHash: {newKey.HashValue}"));
            console.Out.WriteLine(FormattableString.Invariant($"ApiKeyHashType: {newKey.HashAlgorithm}"));

            return Task.FromResult(0);
        }
    }

    internal sealed class GeneratedApiKey
    {
        private const int KeyLengthBytes = 32;

        public readonly string MonitorApiKey;
        public readonly string HashAlgorithm;
        public readonly string HashValue;

        private GeneratedApiKey(string monitorApiKey, string hashAlgorithm, string hashValue)
        {
            this.MonitorApiKey = monitorApiKey;
            this.HashAlgorithm = hashAlgorithm;
            this.HashValue = hashValue;
        }

        public static GeneratedApiKey Create()
        {
            return GeneratedApiKey.Create("SHA256");
        }

        private static GeneratedApiKey Create(string hashAlgorithm)
        {
            using RandomNumberGenerator rng = RandomNumberGenerator.Create();
            using HashAlgorithm hasher = SHA256.Create();

            byte[] secret = new byte[KeyLengthBytes];
            rng.GetBytes(secret);

            byte[] hash = hasher.ComputeHash(secret);
            StringBuilder outHash = new StringBuilder(hash.Length * 2);

            foreach (byte b in hash)
            {
                outHash.AppendFormat("{0:X2}", b);
            }

            string apiKey = Convert.ToBase64String(secret);

            GeneratedApiKey result = new GeneratedApiKey(apiKey, hashAlgorithm, outHash.ToString());
            return result;
        }
    }
}
