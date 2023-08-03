// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Options;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions
{
    internal static class CollectExceptionsOptionsExtensions
    {
        public static ExceptionFormat GetFormat(this CollectExceptionsOptions options) => options.Format.GetValueOrDefault(CollectExceptionsOptionsDefaults.Format);
    }
}
