// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "corhlpr.h"
#include "macros.h"
#include "ProbeInstrumentation.h"

using namespace std;

#define IfFailLogRet(EXPR) IfFailLogRet_(m_pLogger, EXPR)

ProbeInstrumentation::ProbeInstrumentation(const shared_ptr<ILogger>& logger, ICorProfilerInfo12* profilerInfo) :
    m_pCorProfilerInfo(profilerInfo),
    m_pLogger(logger),
    m_probeFunctionId(0),
    m_pAssemblyProbePrep(nullptr)
{
}

HRESULT ProbeInstrumentation::RegisterFunctionProbe(FunctionID enterProbeId)
{
    lock_guard<mutex> lock(m_probePinningMutex);

    if (HasRegisteredProbe())
    {
        m_pLogger->Log(LogLevel::Debug, _LS("Received probes but they have already been registered"));
        return E_FAIL;
    }

    m_pLogger->Log(LogLevel::Debug, _LS("Received probes"));

    m_pAssemblyProbePrep.reset(new (nothrow) AssemblyProbePrep(m_pCorProfilerInfo, enterProbeId));
    IfNullRet(m_pAssemblyProbePrep);

    // JSFIX: Validate the probe's signature before pinning it.
    m_probeFunctionId = enterProbeId;

    return S_OK;
}

HRESULT ProbeInstrumentation::InitBackgroundService()
{
    m_probeManagementThread = thread(&ProbeInstrumentation::WorkerThread, this);
    return S_OK;
}

void ProbeInstrumentation::WorkerThread()
{
    HRESULT hr = m_pCorProfilerInfo->InitializeCurrentThread();
    if (FAILED(hr))
    {
        m_pLogger->Log(LogLevel::Error, _LS("Unable to initialize thread: 0x%08x"), hr);
        return;
    }

    while (true)
    {
        PROBE_WORKER_PAYLOAD payload;
        hr = m_probeManagementQueue.BlockingDequeue(payload);
        if (hr != S_OK)
        {
            break;
        }

        switch (payload.instruction)
        {
        case ProbeWorkerInstruction::INSTALL_PROBES:
            hr = InstallProbes(payload.requests);
            if (hr != S_OK)
            {
                m_pLogger->Log(LogLevel::Error, _LS("Failed to install probes: 0x%08x"), hr);
            }
            break;

        case ProbeWorkerInstruction::UNINSTALL_PROBES:
            hr = UninstallProbes();
            if (hr != S_OK)
            {
                m_pLogger->Log(LogLevel::Error, _LS("Failed to uninstall probes: 0x%08x"), hr);
            }
            break;

        default:
            m_pLogger->Log(LogLevel::Error, _LS("Unknown message"));
            break;
        }
    }
}

void ProbeInstrumentation::ShutdownBackgroundService()
{
    m_probeManagementQueue.Complete();
    m_probeManagementThread.join();
}

HRESULT ProbeInstrumentation::RequestFunctionProbeInstallation(
    ULONG64 functionIds[],
    ULONG32 count,
    ULONG32 argumentBoxingTypes[],
    ULONG32 argumentCounts[])
{
    m_pLogger->Log(LogLevel::Debug, _LS("Probe installation requested"));

    if (!HasRegisteredProbe())
    {
        return S_FALSE;
    }

    //
    // This method receives N (where n is "count") function IDs that probes should be installed into.
    //
    // Along with this, boxing types are provided for every argument in all of the functions, and the number of 
    // arguments for each function can be found using argumentCounts.
    //
    // The boxing types are passed in as a flattened multidimensional array (argumentBoxingTypes).
    //
    //

    //
    // This method un-flattens the passed in data, reconstructing it into an easier-to-understand format
    // before passing off the request to the worker thread.
    //

    START_NO_OOM_THROW_REGION;

    vector<UNPROCESSED_INSTRUMENTATION_REQUEST> requests;
    requests.reserve(count);

    ULONG32 offset = 0;
    for (ULONG32 i = 0; i < count; i++)
    {
        if (UINT32_MAX - offset < argumentCounts[i])
        {
            return E_INVALIDARG;
        }

        vector<ULONG32> tokens;
        tokens.reserve(argumentCounts[i]);
        for (ULONG32 j = 0; j < argumentCounts[i]; j++)
        {
            tokens.push_back(argumentBoxingTypes[offset+j]);
        }
        offset += argumentCounts[i];

        UNPROCESSED_INSTRUMENTATION_REQUEST request;
        request.functionId = static_cast<FunctionID>(functionIds[i]);
        request.boxingTypes = tokens;

        requests.push_back(request);
    }

    m_probeManagementQueue.Enqueue({ProbeWorkerInstruction::INSTALL_PROBES, requests});

    END_NO_OOM_THROW_REGION;

    return S_OK;
}

HRESULT ProbeInstrumentation::RequestFunctionProbeUninstallation()
{
    m_pLogger->Log(LogLevel::Debug, _LS("Probe uninstallation requested"));

    if (!HasRegisteredProbe())
    {
        return S_FALSE;
    }

    PROBE_WORKER_PAYLOAD payload = {};
    payload.instruction = ProbeWorkerInstruction::UNINSTALL_PROBES;
    m_probeManagementQueue.Enqueue(payload);

    return S_OK;
}

bool ProbeInstrumentation::HasRegisteredProbe()
{
    return m_probeFunctionId != 0;
}

HRESULT ProbeInstrumentation::InstallProbes(vector<UNPROCESSED_INSTRUMENTATION_REQUEST>& requests)
{
    HRESULT hr;

    lock_guard<mutex> lock(m_instrumentationProcessingMutex);

    if (!HasRegisteredProbe() ||
        AreProbesInstalled())
    {
        return E_FAIL;
    }

    START_NO_OOM_THROW_REGION;

    unordered_map<pair<ModuleID, mdMethodDef>, INSTRUMENTATION_REQUEST, PairHash<ModuleID, mdMethodDef>> newRequests;

    vector<ModuleID> requestedModuleIds;
    vector<mdMethodDef> requestedMethodDefs;

    requestedModuleIds.reserve(requests.size());
    requestedMethodDefs.reserve(requests.size());

    for (auto const& req : requests)
    {
        INSTRUMENTATION_REQUEST processedRequest;

        // For now just use the function id as the uniquifier.
        // Consider allowing the caller to specify one.
        processedRequest.uniquifier = static_cast<ULONG64>(req.functionId);
        processedRequest.boxingTypes = req.boxingTypes;

        IfFailLogRet(m_pCorProfilerInfo->GetFunctionInfo2(
            req.functionId,
            NULL,
            nullptr,
            &processedRequest.moduleId,
            &processedRequest.methodDef,
            0,
            nullptr,
            nullptr));

        IfFailLogRet(m_pAssemblyProbePrep->PrepareAssemblyForProbes(processedRequest.moduleId));

        requestedModuleIds.push_back(processedRequest.moduleId);
        requestedMethodDefs.push_back(processedRequest.methodDef);

        if (!m_pAssemblyProbePrep->TryGetAssemblyPrepData(processedRequest.moduleId, processedRequest.pAssemblyData))
        {
            return E_UNEXPECTED;
        }

        newRequests.insert({{processedRequest.moduleId, processedRequest.methodDef}, processedRequest});
    }

    IfFailLogRet(m_pCorProfilerInfo->RequestReJITWithInliners(
        COR_PRF_REJIT_BLOCK_INLINING | COR_PRF_REJIT_INLINING_CALLBACKS,
        static_cast<ULONG>(requestedModuleIds.size()),
        requestedModuleIds.data(),
        requestedMethodDefs.data()));

    m_activeInstrumentationRequests = newRequests;

    END_NO_OOM_THROW_REGION;

    return S_OK;
}

HRESULT ProbeInstrumentation::UninstallProbes()
{
    HRESULT hr;

    lock_guard<mutex> lock(m_instrumentationProcessingMutex);

    if (!HasRegisteredProbe() ||
        !AreProbesInstalled())
    {
        return S_FALSE;
    }

    START_NO_OOM_THROW_REGION;

    vector<ModuleID> moduleIds;
    vector<mdMethodDef> methodDefs;

    moduleIds.reserve(m_activeInstrumentationRequests.size());
    methodDefs.reserve(m_activeInstrumentationRequests.size());

    for (auto const& requestData: m_activeInstrumentationRequests)
    {
        auto const& methodInfo = requestData.first;
        moduleIds.push_back(methodInfo.first);
        methodDefs.push_back(methodInfo.second);
    }

    IfFailLogRet(m_pCorProfilerInfo->RequestRevert(
        static_cast<ULONG>(moduleIds.size()),
        moduleIds.data(),
        methodDefs.data(),
        nullptr));

    m_activeInstrumentationRequests.clear();

    END_NO_OOM_THROW_REGION;

    return S_OK;
}

bool ProbeInstrumentation::AreProbesInstalled()
{
    return !m_activeInstrumentationRequests.empty();
}

void ProbeInstrumentation::AddProfilerEventMask(DWORD& eventsLow)
{
    eventsLow |= COR_PRF_MONITOR::COR_PRF_ENABLE_REJIT;
}

HRESULT STDMETHODCALLTYPE ProbeInstrumentation::GetReJITParameters(ModuleID moduleId, mdMethodDef methodDef, ICorProfilerFunctionControl* pFunctionControl)
{
    HRESULT hr;

    INSTRUMENTATION_REQUEST request;
    {
        lock_guard<mutex> lock(m_instrumentationProcessingMutex);
        auto const& it = m_activeInstrumentationRequests.find({moduleId, methodDef});
        if (it == m_activeInstrumentationRequests.end())
        {
            return E_FAIL;
        }
        request = it->second;
    }

    hr = ProbeInjector::InstallProbe(
        m_pCorProfilerInfo,
        pFunctionControl,
        request);

    if (FAILED(hr))
    {
        m_pLogger->Log(LogLevel::Error, _LS("Failed to install probes, reverting: 0x%08x"), hr);
        RequestFunctionProbeUninstallation();
        return hr;
    }

    return S_OK;
}