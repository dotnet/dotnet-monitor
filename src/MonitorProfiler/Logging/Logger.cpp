// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "Logger.h"
#include "LoggerHelper.h"
#include "macros.h"

using namespace std;

const LCHAR* ILogger::ConvertArg(const char* str, std::vector<lstring>& argStrings)
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

STDMETHODIMP ILogger::LogV(LogLevel level, const LCHAR* format, ...)
{
    // CONSIDER: The current approach of formatting the format string before
    // sending it off to the individual logger implementations prevents
    // structured logging. The was already precluded because of the use of va_list
    // without knowledge of the arguments types. If structured logging is desired,
    // consider some way of capturing individual format arguments and passing
    // their information and values in an alternative manner than using va_list.
    HRESULT hr = S_OK;

    LCHAR message[MaxEntrySize] = {};

    va_list args;
    va_start(args, format);

    hr = LoggerHelper::FormatTruncate(message, format, args);

    va_end(args);

    IfFailRet(hr);

    IfFailRet(Log(level, message));

    return S_OK;
}
