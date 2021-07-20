// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class GeneratedApiKey
    {
        public const int DefaultKeyLength = 32;
        public const string DefaultHashAlgorithm = "SHA256";

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
            return GeneratedApiKey.Create(DefaultKeyLength, DefaultHashAlgorithm);
        }

        public static GeneratedApiKey Create(int keyLength, string hashAlgorithm)
        {
            if (!HashAlgorithmChecker.IsAllowedAlgorithm(hashAlgorithm))
            {
                throw new ArgumentOutOfRangeException(nameof(hashAlgorithm));
            }

            if (keyLength < ApiKeyAuthenticationHandler.ApiKeyByteMinLength || keyLength > ApiKeyAuthenticationHandler.ApiKeyByteMaxLength)
            {
                throw new ArgumentOutOfRangeException(nameof(keyLength));
            }

            using RandomNumberGenerator rng = RandomNumberGenerator.Create();
            using HashAlgorithm hasher = System.Security.Cryptography.HashAlgorithm.Create(hashAlgorithm);

            byte[] secret = new byte[keyLength];
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
