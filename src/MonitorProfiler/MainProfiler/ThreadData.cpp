// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef DOTNETMONITOR_FEATURE_EXCEPTIONS
#include "ThreadData.h"
#include "macros.h"

using namespace std;

#define IfFalseLogRet(EXPR, hr) IfFalseLogRet_(_logger, EXPR, hr)

ThreadData::ThreadData(const shared_ptr<ILogger>& logger) :
    _exceptionCatcherFunctionId(NoFunctionId),
    _hasException(false),
    _logger(logger)
{
}

void ThreadData::ClearException()
{
    _exceptionCatcherFunctionId = NoFunctionId;
    _hasException = false;
}

HRESULT ThreadData::GetException(bool* hasException, FunctionID* catcherFunctionId)
{
    ExpectedPtr(hasException);
    ExpectedPtr(catcherFunctionId);

    *hasException = _hasException;
    *catcherFunctionId = _exceptionCatcherFunctionId;

    return S_OK;
}

HRESULT ThreadData::SetHasException()
{
    IfFalseLogRet(!_hasException, E_UNEXPECTED);
    IfFalseLogRet(NoFunctionId == _exceptionCatcherFunctionId, E_UNEXPECTED);

    _hasException = true;

    return S_OK;
}

HRESULT ThreadData::SetExceptionCatcherFunction(FunctionID functionId)
{
    IfFalseLogRet(NoFunctionId != functionId, E_INVALIDARG);
    IfFalseLogRet(_hasException, E_UNEXPECTED);

    _exceptionCatcherFunctionId = functionId;

    return S_OK;
}
#endif // DOTNETMONITOR_FEATURE_EXCEPTIONS
