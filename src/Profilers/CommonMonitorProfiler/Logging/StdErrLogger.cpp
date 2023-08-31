// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "StdErrLogger.h"
#include "LoggerHelper.h"
#include "LogLevelHelper.h"
#include "../Environment/EnvironmentHelper.h"
#include "NullLogger.h"
#include "macros.h"

using namespace std;

StdErrLogger::StdErrLogger(const shared_ptr<IEnvironment>& pEnvironment)
{
    // Try to get log level from environment

    EnvironmentHelper helper(pEnvironment, NullLogger::Instance);
    if (FAILED(helper.GetStdErrLoggerLevel(_level)))
    {
        // Fallback to default level
        _level = DefaultLevel;
    }
}

STDMETHODIMP_(bool) StdErrLogger::IsEnabled(LogLevel level)
{
    return LogLevelHelper::IsEnabled(level, _level);
}

STDMETHODIMP StdErrLogger::Log(LogLevel level, const lstring& message)
{
    if (!IsEnabled(level))
    {
        return S_FALSE;
    }

    HRESULT hr = S_OK;

    lstring levelStr;
    IfFailRet(LogLevelHelper::GetShortName(level, levelStr));

    IfFailRet(LoggerHelper::Write(
        stderr,
        _LS("[profiler]%s: %s\n"),
        levelStr.c_str(),
        message.c_str()));

    return S_OK;
}
