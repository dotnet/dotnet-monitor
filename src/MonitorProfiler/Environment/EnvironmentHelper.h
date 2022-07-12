// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include <memory>
#include "Environment.h"
#include "../Logging/Logger.h"

#ifndef ERROR_ENVVAR_NOT_FOUND
#define ERROR_ENVVAR_NOT_FOUND 203L
#endif

/// <summary>
/// Helper class for getting and setting known environment variables.
/// </summary>
class EnvironmentHelper final
{
private:
    static constexpr LPCWSTR DebugLoggerLevelEnvVar = _T("DotnetMonitorProfiler_DebugLogger_Level");
    static constexpr LPCWSTR ProfilerVersionEnvVar = _T("DotnetMonitorProfiler_ProductVersion");
    static constexpr LPCWSTR RuntimeInstanceEnvVar = _T("DotnetMonitorProfiler_RuntimeId");
    static constexpr LPCWSTR StdErrLoggerLevelEnvVar = _T("DotnetMonitorProfiler_StdErrLogger_Level");

    std::shared_ptr<IEnvironment> _environment;
    std::shared_ptr<ILogger> _logger;

#if TARGET_UNIX
    static constexpr LPCWSTR DefaultTempFolder = _T("/tmp");
#endif

    static constexpr LPCWSTR TempEnvVar =
#if TARGET_WINDOWS
    _T("TEMP");
#else
    _T("TMPDIR");
#endif

public:

    EnvironmentHelper(const std::shared_ptr<IEnvironment>& pEnvironment,
        const std::shared_ptr<ILogger>& pLogger);

    /// <summary>
    /// Gets the log level for the debug logger from the environment.
    /// </summary>
    HRESULT GetDebugLoggerLevel(LogLevel& level);

    /// <summary>
    /// Sets the product version environment variable in the specified environment.
    /// </summary>
    HRESULT SetProductVersion();

    HRESULT GetRuntimeInstanceId(tstring& instanceId);

    /// <summary>
    /// Gets the log level for the stderr logger from the environment.
    /// </summary>
    HRESULT GetStdErrLoggerLevel(LogLevel& level);

    HRESULT GetTempFolder(tstring& tempFolder);
};
