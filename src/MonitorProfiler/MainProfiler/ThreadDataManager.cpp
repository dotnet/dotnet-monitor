// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "ThreadDataManager.h"
#include "macros.h"
#include <utility>

using namespace std;

#define IfFailLogRet(EXPR) IfFailLogRet_(_logger, EXPR)

typedef unordered_map<ThreadID, shared_ptr<ThreadData>>::iterator DataMapIterator;

ThreadDataManager::ThreadDataManager(const shared_ptr<ILogger>& logger)
{
    _logger = logger;
}

void ThreadDataManager::AddProfilerEventMask(DWORD& eventsLow)
{
    eventsLow |= COR_PRF_MONITOR::COR_PRF_MONITOR_THREADS;
    eventsLow |= COR_PRF_MONITOR::COR_PRF_MONITOR_EXCEPTIONS;
    eventsLow |= COR_PRF_MONITOR::COR_PRF_MONITOR_GC;
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

    lock_guard<mutex> lock(threadData->GetMutex());

    threadData->ClearException();

    return S_OK;
}

HRESULT ThreadDataManager::GetException(ThreadID threadId, ObjectID* objectId, FunctionID* catcherFunctionId)
{
    ExpectedPtr(objectId);
    ExpectedPtr(catcherFunctionId);

    HRESULT hr = S_OK;

    shared_ptr<ThreadData> threadData;
    IfFailLogRet(GetThreadData(threadId, threadData));

    lock_guard<mutex> lock(threadData->GetMutex());

    IfFailLogRet(threadData->GetException(objectId, catcherFunctionId));

    return ThreadData::NoExceptionId == *objectId ? S_FALSE : S_OK;
}

HRESULT ThreadDataManager::SetExceptionObject(ThreadID threadId, ObjectID objectId)
{
    HRESULT hr = S_OK;

    shared_ptr<ThreadData> threadData;
    IfFailLogRet(GetThreadData(threadId, threadData));

    lock_guard<mutex> lock(threadData->GetMutex());

    IfFailLogRet(threadData->SetExceptionObject(objectId));

    return S_OK;
}

HRESULT ThreadDataManager::SetExceptionCatcherFunction(ThreadID threadId, FunctionID catcherFunctionId)
{
    HRESULT hr = S_OK;

    shared_ptr<ThreadData> threadData;
    IfFailLogRet(GetThreadData(threadId, threadData));

    lock_guard<mutex> lock(threadData->GetMutex());

    IfFailLogRet(threadData->SetExceptionCatcherFunction(catcherFunctionId));

    return S_OK;
}

HRESULT ThreadDataManager::MovedReferences(ULONG cMovedObjectIDRanges, ObjectID oldObjectIDRangeStart[], ObjectID newObjectIDRangeStart[], SIZE_T cObjectIDRangeLength[])
{
    HRESULT hr = S_OK;

    lock_guard<mutex> mapLock(_dataMapMutex);

    // Update all of the exception ObjectIDs for each thread
    for (pair<const ThreadID, shared_ptr<ThreadData>>& pair : _dataMap)
    {
        shared_ptr<ThreadData> threadData = pair.second;
        lock_guard<mutex> lock(threadData->GetMutex());

        ObjectID exceptionObjectId;
        FunctionID exceptionCatcherFunctionId;
        IfFailLogRet(threadData->GetException(&exceptionObjectId, &exceptionCatcherFunctionId));

        // If the thread has an exception associated with it, check if it is one of the compacted
        // ranges. If it is in a range, recalculate the new ObjectID and store it.
        if (ThreadData::NoExceptionId != exceptionObjectId)
        {
            for (ULONG i = 0; i < cMovedObjectIDRanges; i++)
            {
                if (oldObjectIDRangeStart[i] <= exceptionObjectId && exceptionObjectId < (oldObjectIDRangeStart[i] + cObjectIDRangeLength[i]))
                {
                    ObjectID newExceptionObjectId = newObjectIDRangeStart[i] + (exceptionObjectId - oldObjectIDRangeStart[i]);
                    IfFailLogRet(threadData->ExceptionObjectMoved(newExceptionObjectId));
                }
            }
        }
    }

    return S_OK;
}

HRESULT ThreadDataManager::GetThreadData(ThreadID threadId, shared_ptr<ThreadData>& threadData)
{
    lock_guard<mutex> mapLock(_dataMapMutex);

    DataMapIterator iterator = _dataMap.find(threadId);
    if (iterator == _dataMap.end())
    {
        return E_FAIL;
    }

    threadData = iterator->second;

    return S_OK;
}
