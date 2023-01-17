// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "LoggerHelper.h"
#include "macros.h"

using namespace std;

HRESULT LoggerHelper::FormatTruncate(LCHAR* buffer, size_t size, const LCHAR* format, ...)
{
    va_list args;
    va_start(args, format);
    int result = FormatTruncate(buffer, size, format, args);
    va_end(args);
    return result;
}

HRESULT LoggerHelper::FormatTruncate(LCHAR* buffer, size_t size, const LCHAR* format, va_list args)
{
    HRESULT hr = S_OK;

    IfFailRet(SaveRestoreErrno(FormatTruncateImpl, buffer, size, format, args));

    return S_OK;
}

HRESULT LoggerHelper::Write(FILE* stream, const LCHAR* format, ...)
{
    va_list args;
    va_start(args, format);
    
    HRESULT hr = S_OK;

    hr = SaveRestoreErrno(WriteImpl, stream, format, args);

    va_end(args);

    IfFailRet(hr);

    // SaveRestoreErrno returns S_FALSE if failure was indicated but errno was zero.
    if (S_FALSE == hr)
    {
        // Multibyte encoding errors will set the stream to an error state.
        // Get the error indicator from the stream.
        int result = ferror(stream);
        if (0 != result)
        {
            hr = HRESULT_FROM_ERRNO(result);
        }
        else
        {
            // This is an undocumented condition.
            hr = E_UNEXPECTED;
        }
    }

    if (FAILED(hr))
    {
        return hr;
    }

    return S_OK;
}

int LoggerHelper::FormatTruncateImpl(LCHAR* buffer, size_t size, const LCHAR* format, va_list args)
{
#ifdef TARGET_WINDOWS
    return _vsnwprintf_s(
        buffer,
        size,
        _TRUNCATE,
        format,
        args);
#else
    return vsnprintf(
        buffer,
        size,
        format,
        args);
#endif
}

template <typename... T>
HRESULT LoggerHelper::SaveRestoreErrno(int (*func)(T...), T... args)
{
    // Save the errno state before func invocation.
    int previousError = errno;
    errno = 0;

    HRESULT hr = S_OK;
    if (func(args...) < 0)
    {
        if (0 != errno)
        {
            // Create HRESULT from errno.
            hr = HRESULT_FROM_ERRNO(errno);
        }
        else
        {
            // An error may not have actually occurred since errno
            // was not set. For example, string formatting APIs will
            // return -1 if truncation occurs, which may not be an error.
            hr = S_FALSE;
        }
    }

    // Restore errno to value before func invocation.
    errno = previousError;

    return hr;
}

int LoggerHelper::WriteImpl(FILE* stream, const LCHAR* format, va_list args)
{
#ifdef TARGET_WINDOWS
    return vfwprintf_s(
        stream,
        format,
        args);
#else
    return vfprintf(
        stream,
        format,
        args);
#endif
}
