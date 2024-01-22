// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "ProfilerBase.h"
#include "Environment/Environment.h"
#include "Environment/EnvironmentHelper.h"
#include "Logging/Logger.h"
#include "CommonUtilities/ThreadNameCache.h"
#include "ProbeInstrumentation/ProbeInstrumentation.h"
#include <memory>

class MutatingMonitorProfiler final :
    public ProfilerBase
{
private:
    static constexpr LPCWSTR ProfilerVersionEnvVar = _T("DotnetMonitor_MutatingMonitorProfiler_ProductVersion");
    static constexpr LPCWSTR EnableParameterCapturingEnvVar = _T("DotnetMonitor_InProcessFeatures_ParameterCapturing_Enable");

private:
    std::shared_ptr<IEnvironment> m_pEnvironment;
    std::shared_ptr<EnvironmentHelper> _environmentHelper;
    std::shared_ptr<ILogger> m_pLogger;
    std::unique_ptr<ProbeInstrumentation> m_pProbeInstrumentation;

public:
    static GUID GetClsid();

    STDMETHOD(Initialize)(IUnknown* pICorProfilerInfoUnk) override;
    STDMETHOD(Shutdown)() override;
    STDMETHOD(InitializeForAttach)(IUnknown* pCorProfilerInfoUnk, void* pvClientData, UINT cbClientData) override;
    STDMETHOD(LoadAsNotificationOnly)(BOOL *pbNotificationOnly) override;
    STDMETHOD(GetReJITParameters)(ModuleID moduleId, mdMethodDef methodId, ICorProfilerFunctionControl* pFunctionControl) override;

private:
    HRESULT InitializeCommon();
    HRESULT InitializeEnvironment();
    HRESULT InitializeEnvironmentHelper();
};

