﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Tools.Monitor.Egress
{
    internal static class EgressProviderTypes
    {
        public const string AzureBlobStorage = nameof(AzureBlobStorage);

        public const string FileSystem = nameof(FileSystem);

        public const string S3Storage = nameof(S3Storage);
    }
}
