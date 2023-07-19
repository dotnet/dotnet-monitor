// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef DOTNETMONITOR_FEATURE_EXCEPTIONS
#include "ThreadDataManager.h"
#include "macros.h"
#include <utility>

using namespace std;

#define IfFailLogRet(EXPR) IfFailLogRet_(_logger, EXPR)
#define IfFalseLogRet(EXPR, hr) IfFalseLogRet_(_logger, EXPR, hr)

typedef unordered_map<ThreadID, shared_ptr<ThreadData>>::iterator DataMapIterator;

ThreadDataManager::ThreadDataManager(const shared_ptr<ILogger>& logger)
{
    _logger = logger;
}

void ThreadDataManager::AddProfilerEventMask(DWORD& eventsLow)
{
    eventsLow |= COR_PRF_MONITOR::COR_PRF_MONITOR_THREADS;
    eventsLow |= COR_PRF_MONITOR::COR_PRF_MONITOR_EXCEPTIONS;
}

HRESULT ThreadDataManager::ThreadCreated(ThreadID threadId)
{
    lock_guard<mutex> lock(_dataMapMutex);

    _dataMap.insert(make_pair(threadId, make_shared<ThreadData>(_logger)));

    return S_OK;
}

HRESULT ThreadDataManager::ThreadDestroyed(ThreadID threadId)
{
    lock_guard<mutex> lock(_dataMapMutex);

    _dataMap.erase(threadId);

    return S_OK;
}

HRESULT ThreadDataManager::ClearException(ThreadID threadId)
{
    HRESULT hr = S_OK;

    shared_ptr<ThreadData> threadData;
    IfFailLogRet(GetThreadData(threadId, threadData));

    threadData->ClearException();

    return S_OK;
}

HRESULT ThreadDataManager::GetException(ThreadID threadId, bool* hasException, FunctionID* catcherFunctionId)
{
    ExpectedPtr(hasException);
    ExpectedPtr(catcherFunctionId);

    HRESULT hr = S_OK;

    shared_ptr<ThreadData> threadData;
    IfFailLogRet(GetThreadData(threadId, threadData));

    IfFailLogRet(threadData->GetException(hasException, catcherFunctionId));

    return *hasException ? S_FALSE : S_OK;
}

HRESULT ThreadDataManager::SetHasException(ThreadID threadId)
{
    HRESULT hr = S_OK;

    shared_ptr<ThreadData> threadData;
    IfFailLogRet(GetThreadData(threadId, threadData));

    IfFailLogRet(threadData->SetHasException());

    return S_OK;
}

HRESULT ThreadDataManager::SetExceptionCatcherFunction(ThreadID threadId, FunctionID catcherFunctionId)
{
    HRESULT hr = S_OK;

    shared_ptr<ThreadData> threadData;
    IfFailLogRet(GetThreadData(threadId, threadData));

    IfFailLogRet(threadData->SetExceptionCatcherFunction(catcherFunctionId));

    return S_OK;
}

HRESULT ThreadDataManager::GetThreadData(ThreadID threadId, shared_ptr<ThreadData>& threadData)
{
    lock_guard<mutex> mapLock(_dataMapMutex);

    DataMapIterator iterator = _dataMap.find(threadId);
    IfFalseLogRet(iterator != _dataMap.end(), E_UNEXPECTED);

    threadData = iterator->second;

    return S_OK;
}
#endif // DOTNETMONITOR_FEATURE_EXCEPTIONS
