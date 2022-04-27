// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "DebugLogger.h"
#include "LogLevelHelper.h"
#include "../Environment/EnvironmentHelper.h"

using namespace std;

DebugLogger::DebugLogger(const shared_ptr<IEnvironment>& pEnvironment)
{
    // Try to get log level from environment
    if (FAILED(EnvironmentHelper::GetDebugLoggerLevel(pEnvironment, m_level)))
    {
        // Fallback to Information level
        m_level = LogLevel::Information;
    }
}

STDMETHODIMP_(bool) DebugLogger::IsEnabled(LogLevel level)
{
    return LogLevelHelper::IsEnabled(level, m_level);
}

STDMETHODIMP DebugLogger::Log(LogLevel level, const tstring format, va_list args)
{
    if (!IsEnabled(level))
    {
        return S_FALSE;
    }

    HRESULT hr = S_OK;

    WCHAR wszMessage[s_nMaxEntrySize];
    _vsnwprintf_s(
        wszMessage,
        s_nMaxEntrySize,
        _TRUNCATE,
        format.c_str(),
        args);

    tstring tstrLevel;
    if (FAILED(LogLevelHelper::GetShortName(level, tstrLevel)))
    {
        tstrLevel.assign(_T("ukwn"));
    }

    WCHAR wszString[s_nMaxEntrySize];
    _snwprintf_s(
        wszString,
        s_nMaxEntrySize,
        _TRUNCATE,
        _T("[profiler]%s: %s\r\n"),
        tstrLevel.c_str(),
        wszMessage);

    OutputDebugStringW(wszString);

    return S_OK;
}
