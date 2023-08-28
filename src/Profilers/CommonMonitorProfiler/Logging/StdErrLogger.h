// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <memory>
#include <vector>
#include "Logger.h"
#include "../Environment/Environment.h"

/// <summary>
/// Logs messages to the stderr stream.
/// </summary>
class StdErrLogger final :
    public ILogger
{
private:
    const static LogLevel DefaultLevel = LogLevel::None;
    const static size_t MaxEntrySize = 1000;

    LogLevel _level = DefaultLevel;

public:
    StdErrLogger(const std::shared_ptr<IEnvironment>& pEnvironment);

public:
    // ILogger Members

    /// <inheritdoc />
    STDMETHOD_(bool, IsEnabled)(LogLevel level) override;

    /// <inheritdoc />
    STDMETHOD(Log)(LogLevel level, const lstring& message) override;
};
