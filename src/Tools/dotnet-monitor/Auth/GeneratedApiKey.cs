// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class GeneratedApiKey
    {
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

            byte[] secret = new byte[ApiKeyAuthenticationHandler.ApiKeyNumBytes];
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
