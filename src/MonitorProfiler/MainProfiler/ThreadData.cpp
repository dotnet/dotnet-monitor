// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "ThreadData.h"
#include "macros.h"

using namespace std;

#define IfFalseLogRet(EXPR, hr) IfFalseLogRet_(_logger, EXPR, hr)

ThreadData::ThreadData(const shared_ptr<ILogger>& logger) :
    _exceptionCatcherFunctionId(NoFunctionId),
    _exceptionObjectId(NoExceptionId),
    _logger(logger)
{
}

mutex& ThreadData::GetMutex()
{
    return _mutex;
}

void ThreadData::ClearException()
{
    _exceptionCatcherFunctionId = NoFunctionId;
    _exceptionObjectId = NoExceptionId;
}

HRESULT ThreadData::ExceptionObjectMoved(ObjectID newObjectId)
{
    IfFalseLogRet(NoExceptionId != newObjectId, E_INVALIDARG);
    IfFalseLogRet(NoExceptionId != _exceptionObjectId, E_UNEXPECTED);

    _exceptionObjectId = newObjectId;

    return S_OK;
}

HRESULT ThreadData::GetException(ObjectID* objectId, FunctionID* catcherFunctionId)
{
    ExpectedPtr(objectId);
    ExpectedPtr(catcherFunctionId);

    *objectId = _exceptionObjectId;
    *catcherFunctionId = _exceptionCatcherFunctionId;

    return S_OK;
}

HRESULT ThreadData::SetExceptionObject(ObjectID objectId)
{
    IfFalseLogRet(NoExceptionId != objectId, E_INVALIDARG);
    IfFalseLogRet(NoExceptionId == _exceptionObjectId, E_UNEXPECTED);
    IfFalseLogRet(NoFunctionId == _exceptionCatcherFunctionId, E_UNEXPECTED);

    _exceptionObjectId = objectId;

    return S_OK;
}

HRESULT ThreadData::SetExceptionCatcherFunction(FunctionID functionId)
{
    IfFalseLogRet(NoFunctionId != functionId, E_INVALIDARG);
    IfFalseLogRet(NoExceptionId != _exceptionObjectId, E_UNEXPECTED);

    _exceptionCatcherFunctionId = functionId;

    return S_OK;
}
