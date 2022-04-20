// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include <string>
#include "corhlpr.h"

/// <summary>
/// Logging severity levels.
/// </summary>
enum class LogLevel
{
    Trace,
    Debug,
    Information,
    Warning,
    Error,
    Critical,
    None
};

/// <summary>
/// Interface for logging messages.
/// </summary>
DECLARE_INTERFACE(ILogger)
{
    /// <summary>
    /// Writes a log message.
    /// </summary>
    STDMETHOD(Log)(LogLevel level, const std::string format, va_list args) PURE;

    inline STDMETHODIMP Log(LogLevel level, const std::string format, ...)
    {
        va_list args;
        va_start(args, format);
        HRESULT hr = Log(level, format, args);
        va_end(args);
        return hr;
    }
};

// Checks if EXPR is a failed HRESULT
// If failed, logs the failure and returns the HRESULT
#define IfFailLogRet_(pLogger, EXPR) \
    do { \
        hr = (EXPR); \
        if(FAILED(hr)) { \
            if (nullptr != pLogger) { \
                pLogger->Log( \
                    LogLevel::Error, \
                    "IfFailLogRet(" #EXPR ") failed in function %s: 0x%08x", \
                    __func__, \
                    hr); \
            } \
            return (hr); \
        } \
    } while (0)

// Checks if EXPR is nullptr
// If nullptr, logs the failure and returns E_POINTER
#define IfNullLogRetPtr_(pLogger, EXPR) \
    do { \
        if(nullptr == (EXPR)) { \
            if (nullptr != pLogger) { \
                pLogger->Log( \
                    LogLevel::Error, \
                    "IfNullLogRetPtr(" #EXPR ") failed in function %s", \
                    __func__); \
            } \
            return E_POINTER; \
        } \
    } while (0)

// Logs a message with arguments at the specified level
// Checks if logging failed, returns the HRESULT if failed
#define LogV_(pLogger, level, format, ...) \
    IfFailRet(pLogger->Log(level, format, __VA_ARGS__))

// Logs a message at the Trace level
#define LogTraceV_(pLogger, format, ...) \
    LogV_(pLogger, LogLevel::Trace, format, __VA_ARGS__)

// Logs a message at the Debug level
#define LogDebugV_(pLogger, format, ...) \
    LogV_(pLogger, LogLevel::Debug, format, __VA_ARGS__)

// Logs a message at the Information level
#define LogInformationV_(pLogger, format, ...) \
    LogV_(pLogger, LogLevel::Information, format, __VA_ARGS__)

// Logs a message at the Warning level
#define LogWarningV_(pLogger, format, ...) \
    LogV_(pLogger, LogLevel::Warning, format, __VA_ARGS__)

// Logs a message at the Error level
#define LogErrorV_(pLogger, format, ...) \
    LogV_(pLogger, LogLevel::Error, format, __VA_ARGS__)

// Logs a message at the Critical level
#define LogCriticalV_(pLogger, format, ...) \
    LogV_(pLogger, LogLevel::Critical, format, __VA_ARGS__)
