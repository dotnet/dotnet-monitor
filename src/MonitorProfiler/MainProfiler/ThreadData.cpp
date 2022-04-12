// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "ThreadData.h"
#include "macros.h"

using namespace std;

#define IfFalseLogRet(EXPR, hr) IfFalseLogRet_(m_pLogger, EXPR, hr)

ThreadData::ThreadData(const shared_ptr<ILogger>& pLogger) :
    m_exceptionCatcherFunctionId(NoFunctionId),
    m_exceptionObjectId(NoExceptionId),
    m_pLogger(pLogger)
{
}

mutex& ThreadData::GetMutex()
{
    return m_mutex;
}

void ThreadData::ClearException()
{
    m_exceptionCatcherFunctionId = NoFunctionId;
    m_exceptionObjectId = NoExceptionId;
}

HRESULT ThreadData::ExceptionObjectMoved(ObjectID newObjectId)
{
    IfFalseLogRet(NoExceptionId != newObjectId, E_INVALIDARG);
    IfFalseLogRet(NoExceptionId != m_exceptionObjectId, E_UNEXPECTED);

    m_exceptionObjectId = newObjectId;

    return S_OK;
}

HRESULT ThreadData::GetException(ObjectID* pObjectId, FunctionID* pCatcherFunctionId)
{
    ExpectedPtr(pObjectId);
    ExpectedPtr(pCatcherFunctionId);

    *pObjectId = m_exceptionObjectId;
    *pCatcherFunctionId = m_exceptionCatcherFunctionId;

    return S_OK;
}

HRESULT ThreadData::SetExceptionObject(ObjectID objectId)
{
    IfFalseLogRet(NoExceptionId != objectId, E_INVALIDARG);
    IfFalseLogRet(NoExceptionId == m_exceptionObjectId, E_UNEXPECTED);
    IfFalseLogRet(NoFunctionId == m_exceptionCatcherFunctionId, E_UNEXPECTED);

    m_exceptionObjectId = objectId;

    return S_OK;
}

HRESULT ThreadData::SetExceptionCatcherFunction(FunctionID functionId)
{
    IfFalseLogRet(NoFunctionId != functionId, E_INVALIDARG);
    IfFalseLogRet(NoExceptionId != m_exceptionObjectId, E_UNEXPECTED);

    m_exceptionCatcherFunctionId = functionId;

    return S_OK;
}
