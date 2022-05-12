// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include <memory>
#include "../Logging/Logger.h"
#include "ThreadDataManager.h"
#include "com.h"

/// <summary>
/// Class for tracking exceptions for a runtime instance.
/// </summary>
class ExceptionTracker
{
private:
    ComPtr<ICorProfilerInfo2> _corProfilerInfo;
    std::shared_ptr<ILogger> _logger;
    std::shared_ptr<ThreadDataManager> _threadDataManager;

public:
    ExceptionTracker(
        const std::shared_ptr<ILogger>& logger,
        const std::shared_ptr<ThreadDataManager> threadDataManager,
        ICorProfilerInfo2* corProfilerInfo);

    /// <summary>
    /// Adds profiler event masks needed by class.
    /// </summary>
    static void AddProfilerEventMask(DWORD& eventsLow);

    // Exceptions
    HRESULT ExceptionThrown(ThreadID threadId, ObjectID objectId);
    HRESULT ExceptionSearchCatcherFound(ThreadID threadId, FunctionID functionId);
    HRESULT ExceptionUnwindFunctionEnter(ThreadID threadId, FunctionID functionId);

    // Garbage Collection
    HRESULT MovedReferences(ULONG cMovedObjectIDRanges, ObjectID oldObjectIDRangeStart[], ObjectID newObjectIDRangeStart[], SIZE_T cObjectIDRangeLength[]);

private:
    HRESULT GetFullyQualifiedMethodName(FunctionID functionId, tstring& name);
    static HRESULT STDMETHODCALLTYPE ExceptionThrownStackSnapshotCallback(
        FunctionID funcId,
        UINT_PTR ip,
        COR_PRF_FRAME_INFO frameInfo,
        ULONG32 contextSize,
        BYTE context[],
        void* clientData);
};
