// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Azure.Monitor.Diagnostics;

/// <summary>
/// Exception indicating that a lease could not be acquired.
/// </summary>
public class LeaseUnavailableException : Exception
{
    public LeaseUnavailableException(string message) : base(message)
    {
    }

    public LeaseUnavailableException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
