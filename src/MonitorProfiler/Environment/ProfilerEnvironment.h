// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include <memory>
#include "Environment.h"
#include "../Logging/Logger.h"
#include "corprof.h"
#include "com.h"

/// <summary>
/// Abstraction for getting and setting environment variables from an ICorProfilerInfo instance.
/// </summary>
/// <remarks>
/// On non-Windows platforms, the runtime will cache the initial environment variable block. Managed
/// caled to Environment::[Get|Set]EnvironmentVariable will only mutate the cache, not the real
/// environment block. Additionally, diagnostic commands for accessing the environment variables
/// only mutate and get from the cache, not the real environment block.
/// 
/// This cache is accessible by profilers via the ICorProfilerInfo11 methods instead of using the
/// native platform environment variable methods.
/// 
/// This class provides an abstraction over the profiler methods to allow getting access to the current
/// environment variables as seen by managed code and by diagnostic services.
/// </remarks>
class ProfilerEnvironment final :
    public IEnvironment
{
private:
    ComPtr<ICorProfilerInfo12> m_pCorProfilerInfo;

public:
    ProfilerEnvironment(ICorProfilerInfo12* pCorProfilerInfo);

public:
    // IEnvironment

    /// <inheritdoc />
    STDMETHOD(GetEnvironmentVariable)(tstring name, tstring& value) override;

    /// <inheritdoc />
    STDMETHOD(SetEnvironmentVariable)(tstring name, tstring value) override;
};
