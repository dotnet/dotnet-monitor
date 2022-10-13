// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "Logger.h"
#include <memory>

/// <summary>
/// Provides an empty logger implementation
/// </summary>
class NullLogger final :
    public ILogger
{
public:
    // ILogger Members

    static std::shared_ptr<NullLogger> Instance;

    /// <inheritdoc />
    STDMETHOD_(bool, IsEnabled)(LogLevel level) override
    {
        return false;
    }

    /// <inheritdoc />
    STDMETHOD(Log)(LogLevel level, const lstring& message) override
    {
        return S_OK;
    }
};
