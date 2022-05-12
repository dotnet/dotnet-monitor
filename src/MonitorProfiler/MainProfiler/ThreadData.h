// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include <memory>
#include <mutex>
#include "corhlpr.h"
#include "corprof.h"
#include "../Logging/Logger.h"

/// <summary>
/// Class representing common data for a single thread.
/// </summary>
class ThreadData
{
public:
    static const ObjectID NoExceptionId = 0;
    static const FunctionID NoFunctionId = 0;

private:
    FunctionID _exceptionCatcherFunctionId;
    ObjectID _exceptionObjectId;
    std::mutex _mutex;
    std::shared_ptr<ILogger> _logger;

public:
    ThreadData(const std::shared_ptr<ILogger>& logger);

    std::mutex& GetMutex();

    // Exceptions
    void ClearException();
    HRESULT ExceptionObjectMoved(ObjectID newObjectId);
    HRESULT GetException(ObjectID* objectId, FunctionID* catcherFunctionId);
    HRESULT SetExceptionObject(ObjectID objectId);
    HRESULT SetExceptionCatcherFunction(FunctionID functionId);
};
