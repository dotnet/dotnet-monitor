// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "DebugLogger.h"
#include "LogLevelHelper.h"

using namespace std;

STDMETHODIMP DebugLogger::Log(LogLevel level, const string format, va_list args)
{
    HRESULT hr = S_OK;

    CHAR szMessage[s_nMaxEntrySize];
    _vsnprintf_s(szMessage, s_nMaxEntrySize, _TRUNCATE, format.c_str(), args);

    string strLevel;
    if (FAILED(LogLevelHelper::GetShortName(level, strLevel)))
    {
        strLevel.assign("ukwn");
    }

    CHAR szString[s_nMaxEntrySize];
    _snprintf_s(szString, s_nMaxEntrySize, _TRUNCATE, "[profiler]%s: %s\r\n", strLevel.c_str(), szMessage);

    OutputDebugStringA(szString);

    return S_OK;
}
