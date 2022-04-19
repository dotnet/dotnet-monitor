// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include <memory>
#include <vector>
#include "Logger.h"

/// <summary>
/// Logs messages to the debug output window when running under a debugger.
/// </summary>
class DebugLogger final :
    public ILogger
{
private:
    const static int s_nMaxEntrySize = 1000;

public:
    // ILogger Members

    /// <inheritdoc />
    STDMETHOD(Log)(LogLevel level, const std::string format, va_list args) override;
};
