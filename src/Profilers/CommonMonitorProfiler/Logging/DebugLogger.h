// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <memory>
#include <vector>
#include "Logger.h"
#include "../Environment/Environment.h"

/// <summary>
/// Logs messages to the debug output window when running under a debugger.
/// </summary>
class DebugLogger final :
    public ILogger
{
private:
    const static LogLevel DefaultLevel = LogLevel::Information;
    const static size_t MaxEntrySize = 1000;

    LogLevel _level = DefaultLevel;

public:
    DebugLogger(const std::shared_ptr<IEnvironment>& environment);

public:
    // ILogger Members

    /// <inheritdoc />
    STDMETHOD_(bool, IsEnabled)(LogLevel level) override;

    /// <inheritdoc />
    STDMETHOD(Log)(LogLevel level, const lstring& message) override;
};
