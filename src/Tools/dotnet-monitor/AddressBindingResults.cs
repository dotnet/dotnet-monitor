// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class AddressBindingResults
    {
        public IList<AddressBindingResult> Errors { get; }
            = new List<AddressBindingResult>();

        public bool AnyBoundPorts { get; set; }
    }

    internal sealed class AddressBindingResult
    {
        public readonly string Message;

        public readonly Exception Exception;

        public AddressBindingResult(string message, Exception exception)
        {
            Message = message;
            Exception = exception;
        }
    }
}
