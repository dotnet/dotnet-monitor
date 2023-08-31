// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Exceptions;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor.Exceptions
{
    internal sealed class ExceptionsOperationFactory :
        IExceptionsOperationFactory
    {
        private IEndpointInfo _endpointInfo;
        private IExceptionsStore _store;

        public ExceptionsOperationFactory(IEndpointInfo endpointInfo, IExceptionsStore store)
        {
            _endpointInfo = endpointInfo ?? throw new ArgumentNullException(nameof(endpointInfo));
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public IArtifactOperation Create(ExceptionFormat format, ExceptionsConfigurationSettings configuration)
        {
            return new ExceptionsOperation(_endpointInfo, _store, format, configuration);
        }
    }
}
