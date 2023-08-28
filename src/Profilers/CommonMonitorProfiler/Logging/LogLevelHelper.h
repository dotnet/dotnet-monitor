// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <string>
#include "Logger.h"

/// <summary>
/// Utility methods for log level.
/// </summary>
class LogLevelHelper final
{
public:
    /// <summary>
    /// Gets the short name of the log level.
    /// </summary>
    static HRESULT GetShortName(LogLevel level, lstring& strName)
    {
        // The log levels are intentionally four characters long
        // to allow for easy horizontal alignment.
        switch (level)
        {
        case LogLevel::Critical:
            strName.assign(_LS("crit"));
            return S_OK;
        case LogLevel::Debug:
            strName.assign(_LS("dbug"));
            return S_OK;
        case LogLevel::Error:
            strName.assign(_LS("fail"));
            return S_OK;
        case LogLevel::Information:
            strName.assign(_LS("info"));
            return S_OK;
        case LogLevel::None:
            strName.assign(_LS("none"));
            return S_OK;
        case LogLevel::Trace:
            strName.assign(_LS("trce"));
            return S_OK;
        case LogLevel::Warning:
            strName.assign(_LS("warn"));
            return S_OK;
        default:
            strName.assign(_LS("ukwn"));
            return S_OK;
        }
    }

    /// <summary>
    /// Checks if level is valid and meets the specified threshold level.
    /// </summary>
    static bool IsEnabled(LogLevel level, LogLevel thresholdLevel)
    {
        if (LogLevel::None == level || LogLevel::None == thresholdLevel)
        {
            return false;
        }

        return LogLevelHelper::IsValid(level) && level >= thresholdLevel;
    }

    /// <summary>
    /// Checks if level is valid.
    /// </summary>
    static bool IsValid(LogLevel level)
    {
        return LogLevel::Trace <= level && level <= LogLevel::None;
    }

    /// <summary>
    /// Converts a string to its corresponding LogLevel
    /// </summary>
    static HRESULT ToLogLevel(const tstring& tstrLevel, LogLevel& level)
    {
        if (0 == tstrLevel.compare(_T("Critical")))
        {
            level = LogLevel::Critical;
        }
        else if (0 == tstrLevel.compare(_T("Debug")))
        {
            level = LogLevel::Debug;
        }
        else if (0 == tstrLevel.compare(_T("Error")))
        {
            level = LogLevel::Error;
        }
        else if (0 == tstrLevel.compare(_T("Information")))
        {
            level = LogLevel::Information;
        }
        else if (0 == tstrLevel.compare(_T("None")))
        {
            level = LogLevel::None;
        }
        else if (0 == tstrLevel.compare(_T("Trace")))
        {
            level = LogLevel::Trace;
        }
        else if (0 == tstrLevel.compare(_T("Warning")))
        {
            level = LogLevel::Warning;
        }
        else
        {
            level = LogLevel::None;
            return E_FAIL;
        }

        return S_OK;
    }
};
