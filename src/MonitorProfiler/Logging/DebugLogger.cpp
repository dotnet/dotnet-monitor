// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "DebugLogger.h"
#include "LoggerHelper.h"
#include "LogLevelHelper.h"
#include "../Environment/EnvironmentHelper.h"
#include "NullLogger.h"
#include "macros.h"

using namespace std;

DebugLogger::DebugLogger(const shared_ptr<IEnvironment>& environment)
{
    // Try to get log level from environment

    EnvironmentHelper helper(environment, NullLogger::Instance);
    if (FAILED(helper.GetDebugLoggerLevel(_level)))
    {
        // Fallback to default level
        _level = DefaultLevel;
    }
}

STDMETHODIMP_(bool) DebugLogger::IsEnabled(LogLevel level)
{
    return LogLevelHelper::IsEnabled(level, _level);
}

STDMETHODIMP DebugLogger::Log(LogLevel level, const lstring& message)
{
    if (!IsEnabled(level))
    {
        return S_FALSE;
    }

    HRESULT hr = S_OK;

    lstring levelStr;
    IfFailRet(LogLevelHelper::GetShortName(level, levelStr));

    WCHAR output[MaxEntrySize] = {};

    IfFailRet(LoggerHelper::FormatTruncate(
        output,
        _T("[profiler]%s: %s\r\n"),
        levelStr.c_str(),
        message.c_str()));

    OutputDebugStringW(output);

    return S_OK;
}
