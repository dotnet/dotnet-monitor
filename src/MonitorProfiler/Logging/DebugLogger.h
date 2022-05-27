// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    const static LogLevel s_DefaultLevel = LogLevel::Information;
    const static int s_nMaxEntrySize = 1000;

    LogLevel m_level = s_DefaultLevel;

public:
    DebugLogger(const std::shared_ptr<IEnvironment>& pEnvironment);

public:
    // ILogger Members

    /// <inheritdoc />
    STDMETHOD_(bool, IsEnabled)(LogLevel level) override;

    /// <inheritdoc />
    STDMETHOD(Log)(LogLevel level, const tstring format, va_list args) override;
};
