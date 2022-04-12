// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "ThreadDataManager.h"
#include "macros.h"
#include <utility>

using namespace std;

#define IfFailLogRet(EXPR) IfFailLogRet_(m_pLogger, EXPR)

typedef unordered_map<ThreadID, shared_ptr<ThreadData>>::iterator DataMapIterator;

ThreadDataManager::ThreadDataManager(const shared_ptr<ILogger>& pLogger)
{
    m_pLogger = pLogger;
}

void ThreadDataManager::AddProfilerEventMask(DWORD& eventsLow)
{
    eventsLow |= COR_PRF_MONITOR::COR_PRF_MONITOR_THREADS;
    eventsLow |= COR_PRF_MONITOR::COR_PRF_MONITOR_EXCEPTIONS;
    eventsLow |= COR_PRF_MONITOR::COR_PRF_MONITOR_GC;
}

HRESULT ThreadDataManager::ThreadCreated(ThreadID threadId)
{
    lock_guard<mutex> lock(m_dataMapMutex);

    m_dataMap.insert(make_pair(threadId, make_shared<ThreadData>(m_pLogger)));

    return S_OK;
}

HRESULT ThreadDataManager::ThreadDestroyed(ThreadID threadId)
{
    lock_guard<mutex> lock(m_dataMapMutex);

    m_dataMap.erase(threadId);

    return S_OK;
}

HRESULT ThreadDataManager::ClearException(ThreadID threadId)
{
    HRESULT hr = S_OK;

    shared_ptr<ThreadData> pThreadData;
    IfFailLogRet(GetThreadData(threadId, pThreadData));

    lock_guard<mutex> lock(pThreadData->GetMutex());

    pThreadData->ClearException();

    return S_OK;
}

HRESULT ThreadDataManager::GetException(ThreadID threadId, ObjectID* pObjectId, FunctionID* pCatcherFunctionId)
{
    ExpectedPtr(pObjectId);
    ExpectedPtr(pCatcherFunctionId);

    HRESULT hr = S_OK;

    shared_ptr<ThreadData> pThreadData;
    IfFailLogRet(GetThreadData(threadId, pThreadData));

    lock_guard<mutex> lock(pThreadData->GetMutex());

    IfFailLogRet(pThreadData->GetException(pObjectId, pCatcherFunctionId));

    return ThreadData::NoExceptionId == *pObjectId ? S_FALSE : S_OK;
}

HRESULT ThreadDataManager::SetExceptionObject(ThreadID threadId, ObjectID objectId)
{
    HRESULT hr = S_OK;

    shared_ptr<ThreadData> pThreadData;
    IfFailLogRet(GetThreadData(threadId, pThreadData));

    lock_guard<mutex> lock(pThreadData->GetMutex());

    IfFailLogRet(pThreadData->SetExceptionObject(objectId));

    return S_OK;
}

HRESULT ThreadDataManager::SetExceptionCatcherFunction(ThreadID threadId, FunctionID handlingFunctionId)
{
    HRESULT hr = S_OK;

    shared_ptr<ThreadData> pThreadData;
    IfFailLogRet(GetThreadData(threadId, pThreadData));

    lock_guard<mutex> lock(pThreadData->GetMutex());

    IfFailLogRet(pThreadData->SetExceptionCatcherFunction(handlingFunctionId));

    return S_OK;
}

HRESULT ThreadDataManager::MovedReferences(ULONG cMovedObjectIDRanges, ObjectID oldObjectIDRangeStart[], ObjectID newObjectIDRangeStart[], SIZE_T cObjectIDRangeLength[])
{
    HRESULT hr = S_OK;

    lock_guard<mutex> mapLock(m_dataMapMutex);

    // Update all of the exception ObjectIDs for each thread
    for (pair<const ThreadID, shared_ptr<ThreadData>>& pair : m_dataMap)
    {
        shared_ptr<ThreadData> pThreadData = pair.second;
        lock_guard<mutex> lock(pThreadData->GetMutex());

        ObjectID exceptionObjectId;
        FunctionID exceptionCatcherFunctionId;
        IfFailLogRet(pThreadData->GetException(&exceptionObjectId, &exceptionCatcherFunctionId));

        // If the thread has an exception associated with it, check if it is one of the compacted
        // ranges. If it is in a range, recalculate the new ObjectID and store it.
        if (ThreadData::NoExceptionId != exceptionObjectId)
        {
            for (ULONG i = 0; i < cMovedObjectIDRanges; i++)
            {
                if (oldObjectIDRangeStart[i] <= exceptionObjectId && exceptionObjectId < (oldObjectIDRangeStart[i] + cObjectIDRangeLength[i]))
                {
                    ObjectID newExceptionObjectId = newObjectIDRangeStart[i] + (exceptionObjectId - oldObjectIDRangeStart[i]);
                    IfFailLogRet(pThreadData->ExceptionObjectMoved(newExceptionObjectId));
                }
            }
        }
    }

    return S_OK;
}

HRESULT ThreadDataManager::GetThreadData(ThreadID threadId, shared_ptr<ThreadData>& pThreadData)
{
    lock_guard<mutex> mapLock(m_dataMapMutex);

    DataMapIterator iterator = m_dataMap.find(threadId);
    if (iterator == m_dataMap.end())
    {
        return E_FAIL;
    }

    pThreadData = iterator->second;

    return S_OK;
}
