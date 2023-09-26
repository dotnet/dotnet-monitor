// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "corhlpr.h"
#include "Logger.h"

class LoggerHelper final
{
public:
    /// <summary>
    /// Write formatted output using a pointer to a list of arguments.
    /// </summary>
    static HRESULT FormatTruncate(
        LCHAR* buffer,
        size_t size,
        const LCHAR* format,
        va_list args);

    /// <summary>
    /// Write formatted output using a pointer to a list of arguments.
    /// </summary>
    template <size_t size>
    inline static HRESULT FormatTruncate(
        LCHAR (&buffer)[size],
        const LCHAR* format,
        va_list args)
    {
        return FormatTruncate(buffer, size, format, args);
    }

    /// <summary>
    /// Writes formatted data to a string.
    /// </summary>
    static HRESULT FormatTruncate(
        LCHAR* buffer,
        size_t size,
        const LCHAR* format,
        ...);

    /// <summary>
    /// Writes formatted data to a string.
    /// </summary>
    template <size_t size>
    inline static HRESULT FormatTruncate(
        LCHAR (&buffer)[size],
        const LCHAR* format,
        ...)
    {
        va_list args;
        va_start(args, format);
        int result = FormatTruncate(buffer, size, format, args);
        va_end(args);
        return result;
    }
    
    /// <summary>
    /// Write formatted output to a stream using a pointer to a list of arguments.
    /// </summary>
    static HRESULT Write(
        FILE* stream,
        const LCHAR* format,
        ...);

private:
    /// <summary>
    /// _vsnwprintf_s / vsnprintf
    /// </summary>
    inline static int FormatTruncateImpl(
        LCHAR* buffer,
        size_t size,
        const LCHAR* format,
        va_list args);

    /// <summary>
    /// Saves the current errno value, executes the function, restores errno, and returns
    /// an HRESULT based on the value of the error reported from the function execution.
    /// </summary>
    template <typename... T>
    static HRESULT SaveRestoreErrno(
        int (*func)(T...),
        T... args);

    /// <summary>
    /// vfwprintf_s / vfprintf
    /// </summary>
    inline static int WriteImpl(
        FILE* stream,
        const LCHAR* format,
        va_list args);
};
