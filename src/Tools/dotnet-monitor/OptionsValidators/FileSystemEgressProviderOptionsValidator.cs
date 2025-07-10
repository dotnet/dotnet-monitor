// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Options;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress.FileSystem
{
    [OptionsValidator]
    internal sealed partial class FileSystemEgressProviderOptionsValidator : IValidateOptions<FileSystemEgressProviderOptions>
    {
        IServiceProvider _serviceProvider;

        public FileSystemEgressProviderOptionsValidator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
    }
}
