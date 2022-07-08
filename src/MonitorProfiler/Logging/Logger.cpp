// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "Logger.h"
#include "macros.h"

using namespace std;

const LCHAR* ILogger::ConvertArg(const char*& str, std::vector<lstring>& argStrings)
{
#ifdef TARGET_WINDOWS
    return ConvertArg(std::string(str), argStrings);
#else
    return str;
#endif
}

const LCHAR* ILogger::ConvertArg(const std::string& str, std::vector<lstring>& argStrings)
{
#ifdef TARGET_WINDOWS
    // Convert the string, place it in the argStrings vector, and return the raw string for formatting.
    return argStrings.emplace(argStrings.end(), to_tstring(str))->c_str();
#else
    // string and lstring have the same width on non-Windows
    return str.c_str();
#endif
}

const LCHAR* ILogger::ConvertArg(const tstring& str, std::vector<lstring>& argStrings)
{
#ifdef TARGET_WINDOWS
    // tstring and lstring have the same width on Windows
    return str.c_str();
#else
    // Convert the string, place it in the argStrings vector, and return the raw string for formatting.
    return argStrings.emplace(argStrings.end(), to_string(str))->c_str();
#endif
}

STDMETHODIMP ILogger::LogV(LogLevel level, const lstring format, ...)
{
    va_list args;
    va_start(args, format);
    LCHAR message[MaxEntrySize];

    // The result of the string formatting APIs will return a negative
    // number when truncation occurs, however this is not an error condition.
    // Clear errno in order to use it to indicate if an actual error occurs.
    int result = 0;
    int previousError = errno;
    errno = 0;

#ifdef TARGET_WINDOWS
    result = _vsnwprintf_s(
        message,
        _TRUNCATE,
        format.c_str(),
        args);
#else
    result = vsnprintf(
        message,
        MaxEntrySize,
        format.c_str(),
        args);
#endif
    va_end(args);

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

    return Log(level, message);
}
