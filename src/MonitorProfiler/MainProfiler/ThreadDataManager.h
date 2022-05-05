// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

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
    std::unordered_map<ThreadID, std::shared_ptr<ThreadData>> m_dataMap;
    std::mutex m_dataMapMutex;
    std::shared_ptr<ILogger> m_pLogger;

public:
    ThreadDataManager(const std::shared_ptr<ILogger>& pLogger);

    /// <summary>
    /// Adds profiler event masks needed by class.
    /// </summary>
    static void AddProfilerEventMask(DWORD& eventsLow);

    // Threads
    HRESULT ThreadCreated(ThreadID threadId);
    HRESULT ThreadDestroyed(ThreadID threadId);

    // Exceptions
    HRESULT ClearException(ThreadID threadId);
    HRESULT GetException(ThreadID threadId, ObjectID* pObjectId, FunctionID* pHandlingFunctionId);
    HRESULT SetExceptionObject(ThreadID threadId, ObjectID objectId);
    HRESULT SetExceptionCatcherFunction(ThreadID threadId, FunctionID handlingFunctionId);

    // Garbage Collection
    HRESULT MovedReferences(ULONG cMovedObjectIDRanges, ObjectID oldObjectIDRangeStart[], ObjectID newObjectIDRangeStart[], SIZE_T cObjectIDRangeLength[]);

private:
    HRESULT GetThreadData(ThreadID threadId, std::shared_ptr<ThreadData>& pThreadData);
};
