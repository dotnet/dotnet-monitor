// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "MutatingMonitorProfiler.h"
#include "Environment/EnvironmentHelper.h"
#include "Environment/ProfilerEnvironment.h"
#include "Logging/LoggerFactory.h"
#include "corhlpr.h"
#include "macros.h"
#include <memory>
#include <mutex>

using namespace std;

#define IfFailLogRet(EXPR) IfFailLogRet_(m_pLogger, EXPR)

GUID MutatingMonitorProfiler::GetClsid()
{
    // {38759DC4-0685-4771-AD09-A7627CE1B3B4}
    return { 0x38759dc4, 0x685, 0x4771, { 0xad, 0x9, 0xa7, 0x62, 0x7c, 0xe1, 0xb3, 0xb4 } };
}

STDMETHODIMP MutatingMonitorProfiler::Initialize(IUnknown *pICorProfilerInfoUnk)
{
    ExpectedPtr(pICorProfilerInfoUnk);

    HRESULT hr = S_OK;

    // These should always be initialized first
    IfFailRet(ProfilerBase::Initialize(pICorProfilerInfoUnk));

    IfFailRet(InitializeCommon());

    return S_OK;
}

STDMETHODIMP MutatingMonitorProfiler::Shutdown()
{
    m_pLogger.reset();
    m_pEnvironment.reset();

    if (m_pProbeInstrumentation)
    {
        m_pProbeInstrumentation->ShutdownBackgroundService();
        m_pProbeInstrumentation.reset();
    }

    return ProfilerBase::Shutdown();
}

STDMETHODIMP MutatingMonitorProfiler::InitializeForAttach(IUnknown* pCorProfilerInfoUnk, void* pvClientData, UINT cbClientData)
{
    HRESULT hr = S_OK;

    // These should always be initialized first
    IfFailRet(ProfilerBase::Initialize(pCorProfilerInfoUnk));

    IfFailRet(InitializeCommon());

    return S_OK;
}

STDMETHODIMP MutatingMonitorProfiler::LoadAsNotificationOnly(BOOL *pbNotificationOnly)
{
    ExpectedPtr(pbNotificationOnly);

    *pbNotificationOnly = FALSE;

    return S_OK;
}

HRESULT MutatingMonitorProfiler::InitializeCommon()
{
    HRESULT hr = S_OK;

    // These are created in dependency order!
    IfFailRet(InitializeEnvironment());
    IfFailRet(LoggerFactory::Create(m_pEnvironment, m_pLogger));
    IfFailRet(InitializeEnvironmentHelper());

    // Logging is initialized and can now be used
    bool supported;
    IfFailLogRet(ProfilerBase::IsRuntimeSupported(supported));
    if (!supported)
    {
        m_pLogger->Log(LogLevel::Debug, _LS("Unsupported runtime."));
        return CORPROF_E_PROFILER_CANCEL_ACTIVATION;
    }

    // Set product version environment variable to allow discovery of if the profiler
    // as been applied to a target process. Diagnostic tools must use the diagnostic
    // communication channel's GetProcessEnvironment command to get this value.
    IfFailLogRet(_environmentHelper->SetProductVersion(ProfilerVersionEnvVar));

    DWORD eventsLow = COR_PRF_MONITOR::COR_PRF_MONITOR_NONE;

    bool enableParameterCapturing;
    IfFailLogRet(_environmentHelper->GetIsFeatureEnabled(EnableParameterCapturingEnvVar, enableParameterCapturing));
    if (enableParameterCapturing)
    {
        m_pProbeInstrumentation.reset(new (nothrow) ProbeInstrumentation(m_pLogger, m_pCorProfilerInfo));
        IfNullRet(m_pProbeInstrumentation);
        m_pProbeInstrumentation->AddProfilerEventMask(eventsLow);
    }
    else
    {
        ProbeInstrumentation::DisableIncomingRequests();
    }

    IfFailRet(m_pCorProfilerInfo->SetEventMask2(
        eventsLow,
        COR_PRF_HIGH_MONITOR::COR_PRF_HIGH_MONITOR_NONE));

    if (enableParameterCapturing)
    {
        IfFailLogRet(m_pProbeInstrumentation->InitBackgroundService());
    }

    return S_OK;
}

HRESULT MutatingMonitorProfiler::InitializeEnvironment()
{
    if (m_pEnvironment)
    {
        return E_UNEXPECTED;
    }
    m_pEnvironment = make_shared<ProfilerEnvironment>(m_pCorProfilerInfo);
    return S_OK;
}

HRESULT MutatingMonitorProfiler::InitializeEnvironmentHelper()
{
    IfNullRet(m_pEnvironment);

    _environmentHelper = make_shared<EnvironmentHelper>(m_pEnvironment, m_pLogger);

    return S_OK;
}

HRESULT STDMETHODCALLTYPE MutatingMonitorProfiler::GetReJITParameters(ModuleID moduleId, mdMethodDef methodId, ICorProfilerFunctionControl* pFunctionControl)
{
    if (m_pProbeInstrumentation)
    {
        return m_pProbeInstrumentation->GetReJITParameters(moduleId, methodId, pFunctionControl);
    }

    return S_OK;
}