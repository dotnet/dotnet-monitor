// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "DebugLogger.h"
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

    WCHAR wszString[MaxEntrySize];

    // The result of the string formatting APIs will return a negative
    // number when truncation occurs, however this is not an error condition.
    // Clear errno in order to use it to indicate if an actual error occurs.
    int previousError = errno;
    errno = 0;

    int result = _snwprintf_s(
        wszString,
        _TRUNCATE,
        _T("[profiler]%s: %s\r\n"),
        levelStr.c_str(),
        message.c_str());

    // Result may be negative if truncation occurs, however this is not an
    // error condition. Check the value of errno before assuming an error
    // occurred.
    if (result < 0 && errno != 0)
    {
        return HRESULT_FROM_ERRNO(errno);
    }

    // Successful invocations of platform APIs typically do not modify errno
    // if no failure occurs. To maintain this behavior, restore the value of
    // errno prior to invoking the string formatting API.
    errno = previousError;

    OutputDebugStringW(wszString);

    return S_OK;
}
