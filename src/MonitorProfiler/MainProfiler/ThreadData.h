// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

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
    FunctionID m_exceptionCatcherFunctionId;
    ObjectID m_exceptionObjectId;
    std::mutex m_mutex;
    std::shared_ptr<ILogger> m_pLogger;

public:
    ThreadData(const std::shared_ptr<ILogger>& pLogger);

    std::mutex& GetMutex();

    // Exceptions
    void ClearException();
    HRESULT ExceptionObjectMoved(ObjectID newObjectId);
    HRESULT GetException(ObjectID* pObjectId, FunctionID* pHandlingFunctionId);
    HRESULT SetExceptionObject(ObjectID objectId);
    HRESULT SetExceptionCatcherFunction(FunctionID functionId);
};
