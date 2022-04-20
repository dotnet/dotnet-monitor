// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    static HRESULT GetShortName(LogLevel level, tstring& strName)
    {
        // The log levels are intentionally four characters long
        // to allow for easy horizontal alignment.
        switch (level)
        {
        case LogLevel::Critical:
            strName.assign(_T("crit"));
            return S_OK;
        case LogLevel::Debug:
            strName.assign(_T("dbug"));
            return S_OK;
        case LogLevel::Error:
            strName.assign(_T("fail"));
            return S_OK;
        case LogLevel::Information:
            strName.assign(_T("info"));
            return S_OK;
        case LogLevel::None:
            strName.assign(_T("none"));
            return S_OK;
        case LogLevel::Trace:
            strName.assign(_T("trce"));
            return S_OK;
        case LogLevel::Warning:
            strName.assign(_T("warn"));
            return S_OK;
        default:
            return E_FAIL;
        }
    }
};
