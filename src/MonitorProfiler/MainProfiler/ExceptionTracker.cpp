// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "ExceptionTracker.h"
#include "../Utilities/NameCache.h"
#include "../Utilities/TypeNameUtilities.h"

using namespace std;

#define IfFailLogRet(EXPR) IfFailLogRet_(_logger, EXPR)
#define IfFalseLogRet(EXPR, hr) IfFalseLogRet_(_logger, EXPR, hr)

#define LogDebugV(format, ...) LogDebugV_(_logger, format, __VA_ARGS__)
#define LogInformationV(format, ...) LogInformationV_(_logger, format, __VA_ARGS__)
#define LogErrorV(format, ...) LogErrorV_(_logger, format, __VA_ARGS__)

/*
    Exception Callbacks

    When a managed exception is thrown, the runtime will invoke the profiler with several
    callback methods during the exception processing phases. For a thrown exception, the
    following callbacks are invoked in the following order:
    - Phase 1: Throwing
      - ExceptionThrown: Invoked when the exception object has been thrown.
    - Phase 2: Finding Exception Catcher. Each frame on the callstack is inspected for a matching handler.
      The following are invoked for each frame starting at the leaf frame (the frame from which the exception is thrown):
      - ExceptionSearchFunctionEnter: Invoked when beginning to inspect a function for a matching handler.
      - If the function has handlers with filters (e.g. C#'s when clause), the following are invoked for each filter:
        - ExceptionSearchFilterEnter: Invoked when beginning to evaluate the filter.
        - ExceptionSearchFilterLeave: Invoked when finishing the evaluation of the filter.
      - ExceptionSearchCatcherFound: Invoked only if a matching filter is found.
      - ExceptionSearchFunctionLeave: Invoked when finishing the inspection of the function for a matching handler.
      If a matching handler was found, the exception catcher finding phase ends after the ExceptionSearchFunctionLeave callback.
    - Phase 3: Unwinding.
      The following are invoked for each frame starting at the leaf frame (the frame from which the exception is thrown):
      - ExceptionUnwindFunctionEnter: Invoked when beginning to unwind a frame.
      If a matching handler was found, the unwind phase ends after the invocation of
      the ExceptionUnwindFunctionEnter callback for the frame that contains the matching handler.
      - ExceptionUnwindFunctionLeave: Invoked when finishing the unwind of a frame.
      If all of the frames are unwound to completion, the process is terminated.

    Unhandled Exception Detection Algorithm

    A subset of the aboved described callbacks is used to determine if an unhandled exception has occurred:
    - ExceptionThrown: Record the originating FunctionID and the existance of the exception.
    - ExceptionSearchCatcherFound: Record that the exception will be handled in a catching FunctionID.
    - ExceptionUnwindFunctionEnter: If the exception was not handled (ExceptionSearchCatcherFound was not invoked),
      then the exception is unhandled at this point. Do some extra bookkeeping in the case when the exception is
      handled so that the exception is cleared once the frame with the corresponding catching FunctionID is encountered.

    Current pitfalls:
    - Algorithm doesn't account for exceptions thrown within exception filters. These will be seen as a new set of
      callback invocations starting with throwing, searching, and unwinding, however, the callstack does not unwind
      out of the exception filter and the original exception processing is resumed.
    - Algorithm doesn't account for exceptions thrown within finally blocks. Presumably, these will be seen as new set
      of callback invocations starting with throwing, searching, and unwinding. The original exception processing is
      superceded by the new set of callbacks for the new exception.
*/


ExceptionTracker::ExceptionTracker(
    const shared_ptr<ILogger>& logger,
    const shared_ptr<ThreadDataManager> threadDataManager,
    ICorProfilerInfo12* corProfilerInfo)
{
    _corProfilerInfo = corProfilerInfo;
    _logger = logger;
    _threadDataManager = threadDataManager;
}

void ExceptionTracker::AddProfilerEventMask(DWORD& eventsLow)
{
    if (_logger->IsEnabled(LogLevel::Debug))
    {
        eventsLow |= COR_PRF_MONITOR::COR_PRF_ENABLE_STACK_SNAPSHOT;
    }
}

HRESULT ExceptionTracker::ExceptionThrown(ThreadID threadId, ObjectID objectId)
{
    // CAUTION: Do not store the exception ObjectID. It is not guaranteed to be correct
    // outside of the ExceptionThrown callback without updating it using GC callbacks.

    HRESULT hr = S_OK;

    IfFailLogRet(_threadDataManager->SetHasException(threadId));

    // Exception throwing is common; don't pay to calculate method name if it won't be logged.
    if (_logger->IsEnabled(LogLevel::Debug))
    {
        hr = _corProfilerInfo->DoStackSnapshot(
            threadId,
            LogExceptionThrownFrameCallback,
            COR_PRF_SNAPSHOT_INFO::COR_PRF_SNAPSHOT_DEFAULT,
            this,
            nullptr,
            0);

        if (FAILED(hr) && hr != CORPROF_E_STACKSNAPSHOT_ABORTED)
        {
            LogErrorV("DoStackSnapshot failed in function %s: 0x%08x", __func__, hr);
            return hr;
        }
    }

    return S_OK;
}

HRESULT ExceptionTracker::ExceptionSearchCatcherFound(ThreadID threadId, FunctionID functionId)
{
    HRESULT hr = S_OK;

    IfFailLogRet(_threadDataManager->SetExceptionCatcherFunction(threadId, functionId));

    return S_OK;
}

HRESULT ExceptionTracker::ExceptionUnwindFunctionEnter(ThreadID threadId, FunctionID functionId)
{
    HRESULT hr = S_OK;

    bool hasException = false;
    FunctionID catcherFunctionId = ThreadData::NoFunctionId;
    IfFailLogRet(_threadDataManager->GetException(threadId, &hasException, &catcherFunctionId));
    IfFalseLogRet(hasException, E_UNEXPECTED);

    if (ThreadData::NoFunctionId == catcherFunctionId)
    {
        tstring methodName;
        IfFailLogRet(GetFullyQualifiedMethodName(functionId, methodName));
        LogInformationV("Exception unhandled: %s", methodName);

        // Future: Block thread until collection is initiated of the desired artifact.
        // Possible serialization of some context of the exception and surrounding method
        // information such as locals and parameters.
    }
    else if (functionId == catcherFunctionId)
    {
        IfFailLogRet(_threadDataManager->ClearException(threadId));

        // Exception handling is common; don't pay to calculate method name if it won't be logged.
        if (_logger->IsEnabled(LogLevel::Debug))
        {
            tstring methodName;
            IfFailLogRet(GetFullyQualifiedMethodName(functionId, methodName));
            LogDebugV("Exception handled: %s", methodName);
        }
    }

    return S_OK;
}

HRESULT ExceptionTracker::GetFullyQualifiedMethodName(FunctionID functionId, tstring& fullMethodName)
{
    HRESULT hr = S_OK;

    IfFailRet(GetFullyQualifiedMethodName(functionId, 0, fullMethodName));

    return S_OK;
}

HRESULT ExceptionTracker::GetFullyQualifiedMethodName(FunctionID functionId, COR_PRF_FRAME_INFO frameInfo, tstring& fullMethodName)
{
    HRESULT hr = S_OK;

    NameCache cache;
    TypeNameUtilities typeNameUtilities(_corProfilerInfo);

    IfFailRet(typeNameUtilities.CacheNames(functionId, frameInfo, cache));
    IfFailRet(cache.GetFullyQualifiedName(functionId, fullMethodName));

    return S_OK;
}

HRESULT ExceptionTracker::LogExceptionThrownFrame(FunctionID functionId, COR_PRF_FRAME_INFO frameInfo)
{
    HRESULT hr = S_OK;

    tstring methodName;
    IfFailLogRet(GetFullyQualifiedMethodName(functionId, frameInfo, methodName));
    LogDebugV("Exception thrown: %s", methodName);

    return S_OK;
}

HRESULT ExceptionTracker::LogExceptionThrownFrameCallback(
    FunctionID functionId,
    UINT_PTR ip,
    COR_PRF_FRAME_INFO frameInfo,
    ULONG32 contextSize,
    BYTE context[],
    void* clientData)
{
    if (nullptr == clientData)
    {
        return E_POINTER;
    }

    ExceptionTracker* exceptionTracker = static_cast<ExceptionTracker*>(clientData);

    HRESULT hr = S_OK;

    IfFailRet(exceptionTracker->LogExceptionThrownFrame(functionId, frameInfo));

    // Cancel stack snapshot callbacks after the top frame.
    return S_FALSE;
}
