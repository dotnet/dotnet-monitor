// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Azure;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Queues;
using Microsoft.Diagnostics.Tools.Monitor.Egress.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress
{
    /// <summary>
    /// Egress provider for egressing stream data to an Azure blob storage account.
    /// </summary>
    /// <remarks>
    /// Blobs created through this provider will overwrite existing blobs if they have the same blob name.
    /// </remarks>
    internal partial class ExtensionEgressDiscoverer
        : IExtensionEgressDiscoverer
    {
        private readonly IConfiguration EgressSection;
        //private readonly ILogger<ExternalEgressDiscoverer> Logger;

        public ExtensionEgressDiscoverer(IConfiguration configuration)//, ILogger<ExternalEgressDiscoverer> logger)
        {
            EgressSection = configuration.GetEgressSection();
            //Logger = logger;
        }

        public IEnumerator<string> GetEnumerator()
        {
            return EgressSection.GetChildren().Where(s => !IsBuiltInSection(s)).Select(s => s.Key).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private bool IsBuiltInSection(IConfigurationSection s)
        {
            Type egressType = typeof(EgressOptions);
            return egressType.GetProperty(s.Key) != null || egressType.GetField(s.Key) != null;
        }
    }

    public interface IExtensionEgressDiscoverer : IEnumerable<string>
    {
    }
}