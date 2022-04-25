// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include <memory>
#include "Environment.h"
#include "../Logging/Logger.h"

/// <summary>
/// Helper class for getting and setting known environment variables.
/// </summary>
class EnvironmentHelper final
{
private:
    static constexpr LPCWSTR s_wszProfilerVersionEnvVar = _T("DotnetMonitorProfiler_ProductVersion");

public:
    /// <summary>
    /// Sets the product version environment variable in the specified environment.
    /// </summary>
    static HRESULT SetProductVersion(
        const std::shared_ptr<IEnvironment>& pEnvironment,
        const std::shared_ptr<ILogger>& pLogger);
};
