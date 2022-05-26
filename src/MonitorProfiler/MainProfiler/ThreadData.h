// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include <memory>
#include "corhlpr.h"
#include "corprof.h"
#include "../Logging/Logger.h"

/// <summary>
/// Class representing common data for a single thread.
/// </summary>
class ThreadData
{
public:
    static const FunctionID NoFunctionId = 0;

private:
    FunctionID _exceptionCatcherFunctionId;
    bool _hasException;
    std::shared_ptr<ILogger> _logger;

public:
    ThreadData(const std::shared_ptr<ILogger>& logger);

    // Exceptions
    void ClearException();
    HRESULT GetException(bool* hasException, FunctionID* catcherFunctionId);
    HRESULT SetHasException();
    HRESULT SetExceptionCatcherFunction(FunctionID functionId);
};
