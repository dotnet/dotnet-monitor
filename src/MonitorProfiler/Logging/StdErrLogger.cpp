// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "StdErrLogger.h"
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

    // The result of the stream writing APIs will return a negative
    // number when an error occurs, however errno may or may not be
    // set depending on the type of error. Clear errno in order to
    // use it to indicate if an actual error occurs.
    int result = 0;
    int previousError = errno;
    errno = 0;

#ifdef TARGET_WINDOWS
    result = fwprintf_s(
        stderr,
        L"[profiler]%s: %s\r\n",
        levelStr.c_str(),
        message.c_str());
#else
    result = fprintf(
        stderr,
        "[profiler]%s: %s\r\n",
        levelStr.c_str(),
        message.c_str());
#endif

    if (result < 0)
    {
        // Writing errors will set errno to non-zero value
        if (0 != errno)
        {
            return HRESULT_FROM_ERRNO(errno);
        }
        else
        {
            // errno was not set; restore its previous value.
            errno = previousError;

            // Multibyte encoding errors will set the stream to an error state.
            // Get the error indicator from the stream.
            result = ferror(stderr);
            if (0 != result)
            {
                return HRESULT_FROM_ERRNO(result);
            }
            else
            {
                // This is an undocumented condition.
                return E_UNEXPECTED;
            }
        }
    }

    // Successful invocations of platform APIs typically do not modify errno
    // if no failure occurs. To maintain this behavior, restore the value of
    // errno prior to invoking the string formatting API.
    errno = previousError;

    return S_OK;
}
