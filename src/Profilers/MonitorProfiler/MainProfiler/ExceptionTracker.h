// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#ifdef DOTNETMONITOR_FEATURE_EXCEPTIONS
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
    ComPtr<ICorProfilerInfo12> _corProfilerInfo;
    std::shared_ptr<ILogger> _logger;
    std::shared_ptr<ThreadDataManager> _threadDataManager;

public:
    ExceptionTracker(
        const std::shared_ptr<ILogger>& logger,
        const std::shared_ptr<ThreadDataManager> threadDataManager,
        ICorProfilerInfo12* corProfilerInfo);

    /// <summary>
    /// Adds profiler event masks needed by class.
    /// </summary>
    void AddProfilerEventMask(DWORD& eventsLow);

    // Exceptions
    HRESULT ExceptionThrown(ThreadID threadId, ObjectID objectId);
    HRESULT ExceptionSearchCatcherFound(ThreadID threadId, FunctionID functionId);
    HRESULT ExceptionUnwindFunctionEnter(ThreadID threadId, FunctionID functionId);

private:
    // Method and type name utilities
    HRESULT GetFullyQualifiedTypeName(ClassID classId, tstring& fullTypeName);
    HRESULT GetFullyQualifiedMethodName(FunctionID functionId, tstring& fullMethodName);
    HRESULT GetFullyQualifiedMethodName(FunctionID functionId, COR_PRF_FRAME_INFO frameInfo, tstring& fullMethodName);

    // ExceptionThrown frame logging utilities
    HRESULT LogExceptionThrownFrame(FunctionID functionId, COR_PRF_FRAME_INFO frameInfo);
    static HRESULT STDMETHODCALLTYPE LogExceptionThrownFrameCallback(
        FunctionID functionId,
        UINT_PTR ip,
        COR_PRF_FRAME_INFO frameInfo,
        ULONG32 contextSize,
        BYTE context[],
        void* clientData);
};
#endif // DOTNETMONITOR_FEATURE_EXCEPTIONS
