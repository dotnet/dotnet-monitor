// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <string>
#include <vector>
#include "corhlpr.h"
#include "tstring.h"

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

#ifdef TARGET_WINDOWS
#define _LS(str) L##str
#else
#define _LS(str) str
#endif 

#ifdef TARGET_WINDOWS
typedef std::wstring lstring;
typedef wchar_t LCHAR;
#else
typedef std::string lstring;
typedef char LCHAR;
#endif

/// <summary>
/// Interface for logging messages.
/// </summary>
DECLARE_INTERFACE(ILogger)
{
private:
    const static size_t MaxEntrySize = 1000;

public:
    /// <summary>
    /// Determines if the logger accepts a message at the given LogLevel.
    /// </summary>
    STDMETHOD_(bool, IsEnabled)(LogLevel level) PURE;

    /// <summary>
    /// Writes a log message.
    /// </summary>
    STDMETHOD(Log)(LogLevel level, const lstring& message) PURE;

    template <typename... T>
    inline STDMETHODIMP Log(LogLevel level, const lstring format, const T&... args)
    {
        // A cache of strings that were converted from their original width
        // to the string width for the target platform. This prevents freeing of the
        // converted strings before logging can complete.
        // CONSIDER: Make this a static thread local so that the vector does not
        // need to be allocated for each log call. Reserve the new size before calling
        // LogV and clear it after calling LogV.
        std::vector<lstring> argStrings;
        argStrings.reserve(sizeof...(args));

        // Call LogV method with the pack expansion converting strings to the
        // appropriate string width for the target platform.
        return LogV(level, format.c_str(), ConvertArg(args, argStrings)...);
    }

private:
    /// <summary>
    /// Convert char* strings to logging string width for the target platform.
    /// </summary>
    static const LCHAR* ConvertArg(const char* str, std::vector<lstring>& argStrings);

    /// <summary>
    /// Convert narrow strings to logging string width for the target platform.
    /// </summary>
    static const LCHAR* ConvertArg(const std::string& str, std::vector<lstring>& argStrings);

    /// <summary>
    /// Convert tstrings to logging string width for the target platform.
    /// </summary>
    static const LCHAR* ConvertArg(const tstring& str, std::vector<lstring>& argStrings);

    /// <summary>
    /// Pass through all other argument types as-is.
    /// </summary>
    template <typename T>
    inline static T ConvertArg(const T& value, std::vector<lstring>& argStrings)
    {
        return value;
    }

    /// <summary>
    /// Formats the format string with the variable arguments and calls the Log(level, message) function.
    /// </summary>
    STDMETHODIMP LogV(LogLevel level, const LCHAR* format, ...);
};

// Checks if EXPR is a failed HRESULT
// If failed, logs the failure and returns the HRESULT
#define IfFailLogRet_(pLogger, EXPR) \
    do { \
        hr = (EXPR); \
        if(FAILED(hr)) { \
            if (nullptr != pLogger) { \
                if (pLogger->IsEnabled(LogLevel::Error)) \
                { \
                    pLogger->Log(\
                        LogLevel::Error, \
                        _LS("IfFailLogRet(" #EXPR ") failed in function %s: 0x%08x"), \
                        __func__, \
                        hr); \
                } \
            } \
            return (hr); \
        } \
    } while (0)

// Checks if EXPR is false
// If false, logs the failure and returns the provided HRESULT
#define IfFalseLogRet_(pLogger, EXPR, hr) \
    do { \
        if(!(EXPR)) { \
            if (nullptr != pLogger) { \
                if (pLogger->IsEnabled(LogLevel::Error)) \
                { \
                    pLogger->Log(\
                        LogLevel::Error, \
                        _LS("IfFalseLogRet(" #EXPR ") is false in function %s: 0x%08x"), \
                        __func__, \
                        hr); \
                } \
            } \
            return hr; \
        } \
    } while (0)

// Checks if EXPR is nullptr
// If nullptr, logs the failure and returns E_POINTER
#define IfNullLogRetPtr_(pLogger, EXPR) \
    do { \
        if(nullptr == (EXPR)) { \
            if (nullptr != pLogger) { \
                if (pLogger->IsEnabled(LogLevel::Error)) \
                { \
                    pLogger->Log( \
                        LogLevel::Error, \
                        _LS("IfNullLogRetPtr(" #EXPR ") failed in function %s"), \
                        __func__); \
                } \
            } \
            return E_POINTER; \
        } \
    } while (0)

// Logs a message with arguments at the specified level
// Checks if logging failed, returns the HRESULT if failed
#define LogV_(pLogger, level, format, ...) \
    if (pLogger->IsEnabled(level)) \
    { \
        IfFailRet(pLogger->Log(level, _LS(format), __VA_ARGS__)); \
    }

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
