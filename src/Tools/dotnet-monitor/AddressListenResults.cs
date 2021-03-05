// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class AddressListenResults
    {
        public IList<AddressListenResult> Errors { get; }
            = new List<AddressListenResult>();

        public bool AnyAddresses => (AddressesCount + MetricAddressesCount) > 0;

        public int MetricAddressesCount { get; set; }

        public int AddressesCount { get; set; }
    }

    internal sealed class AddressListenResult
    {
        public readonly string Url;

        public readonly Exception Exception;

        public AddressListenResult(string Url, Exception exception)
        {
            this.Url = Url;
            Exception = exception;
        }
    }
}
