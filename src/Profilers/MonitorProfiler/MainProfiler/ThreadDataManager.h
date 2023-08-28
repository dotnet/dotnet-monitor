// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#ifdef DOTNETMONITOR_FEATURE_EXCEPTIONS
#include <memory>
#include <mutex>
#include <unordered_map>
#include "corhlpr.h"
#include "corprof.h"
#include "ThreadData.h"
#include "../Logging/Logger.h"

/// <summary>
/// Class for managing common thread information.
/// </summary>
class ThreadDataManager
{
private:
    std::unordered_map<ThreadID, std::shared_ptr<ThreadData>> _dataMap;
    std::mutex _dataMapMutex;
    std::shared_ptr<ILogger> _logger;

public:
    ThreadDataManager(const std::shared_ptr<ILogger>& logger);

    /// <summary>
    /// Adds profiler event masks needed by class.
    /// </summary>
    static void AddProfilerEventMask(DWORD& eventsLow);

    // Threads
    HRESULT ThreadCreated(ThreadID threadId);
    HRESULT ThreadDestroyed(ThreadID threadId);

    // Exceptions
    HRESULT ClearException(ThreadID threadId);
    HRESULT GetException(ThreadID threadId, bool* hasException, FunctionID* catcherFunctionId);
    HRESULT SetHasException(ThreadID threadId);
    HRESULT SetExceptionCatcherFunction(ThreadID threadId, FunctionID catcherFunctionId);

private:
    HRESULT GetThreadData(ThreadID threadId, std::shared_ptr<ThreadData>& threadData);
};
#endif // DOTNETMONITOR_FEATURE_EXCEPTIONS
