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
class ProfilerEnvironment final :
    public IEnvironment
{
private:
    ComPtr<ICorProfilerInfo11> m_pCorProfilerInfo;

public:
    ProfilerEnvironment(ICorProfilerInfo11* pCorProfilerInfo);

public:
    // IEnvironment

    /// <inheritdoc />
    STDMETHOD(GetEnvironmentVariable)(tstring name, tstring& value) override;

    /// <inheritdoc />
    STDMETHOD(SetEnvironmentVariable)(tstring name, tstring value) override;
};
