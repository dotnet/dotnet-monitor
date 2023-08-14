// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.Exceptions
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public sealed class CurrentAppDomainFirstChanceExceptionSourceTests
    {
        private readonly Thread _thisThread = Thread.CurrentThread;

        private EventHandler<ExceptionAvailableEventArgs> CreateHandler(List<Exception> reportedExceptions)
        {
            return (sender, args) =>
            {
                if (Thread.CurrentThread == _thisThread)
                {
                    reportedExceptions.Add(args.Exception);
                }
            };
        }

        [Fact]
        public void CurrentAppDomainFirstChanceExceptionSource_ReportsExceptionInsideLifetime()
        {
            List<Exception> reportedExceptions = new();

            Exception thrownException = new();
            using (CurrentAppDomainFirstChanceExceptionSource source = new())
            {
                source.ExceptionAvailable += CreateHandler(reportedExceptions);
                try
                {
                    throw thrownException;
                }
                catch
                {
                }
            }

            Exception singleException = Assert.Single(reportedExceptions);
            Assert.Equal(thrownException, singleException);
        }

        [Fact]
        public void CurrentAppDomainFirstChanceExceptionSource_NoExceptionsOutsideLifetime()
        {
            List<Exception> reportedExceptions = new();

            using (CurrentAppDomainFirstChanceExceptionSource source = new())
            {
                source.ExceptionAvailable += CreateHandler(reportedExceptions);
            }

            try
            {
                throw new Exception();
            }
            catch
            {
            }

            Assert.Empty(reportedExceptions);
        }

        [Fact]
        public void CurrentAppDomainFirstChanceExceptionSource_ReentrancyPrevented()
        {
            bool handled = false;
            EventHandler<ExceptionAvailableEventArgs> handler = (sender, args) =>
            {
                if (Thread.CurrentThread == _thisThread)
                {
                    Assert.False(handled, "Exception handling reentrancy prevention failed.");
                    handled = true;

                    throw new InvalidOperationException();
                }
            };

            Exception thrownException = new();
            using (CurrentAppDomainFirstChanceExceptionSource source = new())
            {
                source.ExceptionAvailable += handler;
                try
                {
                    throw thrownException;
                }
                catch
                {
                }
            }
        }
    }
}
