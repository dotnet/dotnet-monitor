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
    static HRESULT GetShortName(LogLevel level, std::string& strName)
    {
        // The log levels are intentionally four characters long
        // to allow for easy horizontal alignment.
        switch (level)
        {
        case LogLevel::Critical:
            strName.assign("crit");
            return S_OK;
        case LogLevel::Debug:
            strName.assign("dbug");
            return S_OK;
        case LogLevel::Error:
            strName.assign("fail");
            return S_OK;
        case LogLevel::Information:
            strName.assign("info");
            return S_OK;
        case LogLevel::None:
            strName.assign("none");
            return S_OK;
        case LogLevel::Trace:
            strName.assign("trce");
            return S_OK;
        case LogLevel::Warning:
            strName.assign("warn");
            return S_OK;
        default:
            return E_FAIL;
        }
    }
};
