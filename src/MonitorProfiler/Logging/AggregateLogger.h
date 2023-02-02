// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <memory>
#include <vector>
#include "Logger.h"

/// <summary>
/// Aggregates multiple ILogger implementations together in order to dispatch
/// ILogger method invocations to each ILogger implementation.
/// </summary>
class AggregateLogger final :
    public ILogger
{
private:
    std::vector<std::shared_ptr<ILogger>> m_loggers;

public:
    /// <summary>
    /// Adds an ILogger implementation to the aggregation.
    /// </summary>
    STDMETHOD(Add)(const std::shared_ptr<ILogger> pLogger);

public:
    // ILogger Members

    /// <summary>
    /// Determines if any of the registered ILogger implementations have the specified LogLevel enabled.
    /// </summary>
    STDMETHOD_(bool, IsEnabled)(LogLevel level) override;

    /// <summary>
    /// Invokes the Log method on each registered ILogger implementation.
    /// </summary>
    STDMETHOD(Log)(LogLevel level, const lstring& message) override;
};
