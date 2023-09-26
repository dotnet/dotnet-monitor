// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.WebApi.Exceptions;
using Microsoft.Extensions.Options;

namespace Microsoft.Diagnostics.Tools.Monitor.Exceptions
{
    internal sealed class ExceptionsStoreLimitsCallbackFactory :
        IExceptionsStoreCallbackFactory
    {
        private readonly int _topLevelLimit;

        public ExceptionsStoreLimitsCallbackFactory(IOptions<ExceptionsOptions> options)
            : this(options.Value.GetTopLevelLimit())
        {
        }

        internal ExceptionsStoreLimitsCallbackFactory(int topLevelLimit)
        {
            _topLevelLimit = topLevelLimit;
        }

        public IExceptionsStoreCallback Create(IExceptionsStore store)
        {
            return new ExceptionsStoreLimitsCallback(store, _topLevelLimit);
        }
    }
}
