// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "corhlpr.h"
#include "macros.h"
#include "ProbeInstrumentation.h"
#include "CommonUtilities/BlockingQueue.h"

using namespace std;

#define IfFailLogRet(EXPR) IfFailLogRet_(m_pLogger, EXPR)

#ifndef DLLEXPORT
#define DLLEXPORT
#endif

typedef void (STDMETHODCALLTYPE *ProbeRegistrationCallback)(HRESULT);
typedef void (STDMETHODCALLTYPE *ProbeInstallationCallback)(HRESULT);
typedef void (STDMETHODCALLTYPE *ProbeUninstallationCallback)(HRESULT);
typedef void (STDMETHODCALLTYPE *ProbeFaultCallback)(ULONG64);

typedef struct _PROBE_MANAGEMENT_CALLBACKS
{
    ProbeRegistrationCallback pProbeRegistrationCallback;
    ProbeInstallationCallback pProbeInstallationCallback;
    ProbeUninstallationCallback pProbeUninstallationCallback;
    ProbeFaultCallback pProbeFaultCallback;
} PROBE_MANAGEMENT_CALLBACKS;

mutex g_probeManagementCallbacksMutex; // guards g_probeManagementCallbacks
PROBE_MANAGEMENT_CALLBACKS g_probeManagementCallbacks = {};

BlockingQueue<PROBE_WORKER_PAYLOAD> g_probeManagementQueue;

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

    m_pLogger->Log(LogLevel::Debug, _LS("Registering function probe: 0x%08x"), enterProbeId);

    if (HasRegisteredProbe())
    {
        return E_FAIL;
    }

    m_pAssemblyProbePrep.reset(new (nothrow) AssemblyProbePrep(m_pCorProfilerInfo, enterProbeId));
    IfNullRet(m_pAssemblyProbePrep);

    // Consider: Validate the probe's signature before pinning it.
    m_probeFunctionId = enterProbeId;

    return S_OK;
}

HRESULT ProbeInstrumentation::InitBackgroundService()
{
    m_probeManagementThread = thread(&ProbeInstrumentation::WorkerThread, this);
    //
    // Create a dedicated thread for managed callbacks.
    // Performing the callbacks will taint the calling thread preventing it
    // from using certain ICorProfiler APIs marked as unsafe.
    // Those functions will fail with CORPROF_E_UNSUPPORTED_CALL_SEQUENCE.
    //
    m_managedCallbackThread = thread(&ProbeInstrumentation::ManagedCallbackThread, this);
    return S_OK;
}

void ProbeInstrumentation::ManagedCallbackThread()
{
    HRESULT hr = m_pCorProfilerInfo->InitializeCurrentThread();
    if (FAILED(hr))
    {
        m_pLogger->Log(LogLevel::Error, _LS("Unable to initialize thread: 0x%08x"), hr);
        return;
    }

    while (true)
    {
        MANAGED_CALLBACK_REQUEST request;
        hr = m_managedCallbackQueue.BlockingDequeue(request);
        if (hr != S_OK)
        {
            break;
        }

        switch (request.instruction)
        {
        case ProbeWorkerInstruction::REGISTER_PROBE:
            {
                lock_guard<mutex> lock(g_probeManagementCallbacksMutex);
                if (g_probeManagementCallbacks.pProbeRegistrationCallback != nullptr)
                {
                    g_probeManagementCallbacks.pProbeRegistrationCallback(request.payload.hr);
                }
            }
            break;

        case ProbeWorkerInstruction::INSTALL_PROBES:
            {
                lock_guard<mutex> lock(g_probeManagementCallbacksMutex);
                if (g_probeManagementCallbacks.pProbeInstallationCallback != nullptr)
                {
                    g_probeManagementCallbacks.pProbeInstallationCallback(request.payload.hr);
                }
            }
            break;

        case ProbeWorkerInstruction::FAULTING_PROBE:
            {
                lock_guard<mutex> lock(g_probeManagementCallbacksMutex);
                if (g_probeManagementCallbacks.pProbeFaultCallback != nullptr)
                {
                    g_probeManagementCallbacks.pProbeFaultCallback(static_cast<ULONG64>(request.payload.functionId));
                }
            }
            break;

        case ProbeWorkerInstruction::UNINSTALL_PROBES:
            {
                lock_guard<mutex> lock(g_probeManagementCallbacksMutex);
                if (g_probeManagementCallbacks.pProbeUninstallationCallback != nullptr)
                {
                    g_probeManagementCallbacks.pProbeUninstallationCallback(request.payload.hr);
                }
            }
            break;

        default:
            m_pLogger->Log(LogLevel::Error, _LS("Unknown message"));
            break;
        }
    }
}


void ProbeInstrumentation::WorkerThread()
{
    HRESULT hr = m_pCorProfilerInfo->InitializeCurrentThread();
    if (FAILED(hr))
    {
        m_pLogger->Log(LogLevel::Error, _LS("Unable to initialize thread: 0x%08x"), hr);
        return;
    }

    MANAGED_CALLBACK_REQUEST callbackRequest = {};
    while (true)
    {
        PROBE_WORKER_PAYLOAD payload;
        hr = g_probeManagementQueue.BlockingDequeue(payload);
        if (hr != S_OK)
        {
            break;
        }

        callbackRequest.instruction = payload.instruction;
        switch (payload.instruction)
        {
        case ProbeWorkerInstruction::REGISTER_PROBE:
            hr = RegisterFunctionProbe(payload.functionId);
            if (hr != S_OK)
            {
                m_pLogger->Log(LogLevel::Error, _LS("Failed to register function probe: 0x%08x"), hr);
            }
            callbackRequest.payload.hr = hr;
            m_managedCallbackQueue.Enqueue(callbackRequest);
            break;

        case ProbeWorkerInstruction::INSTALL_PROBES:
            hr = InstallProbes(payload.requests);
            if (hr != S_OK)
            {
                m_pLogger->Log(LogLevel::Error, _LS("Failed to install probes: 0x%08x"), hr);
            }
            callbackRequest.payload.hr = hr;
            m_managedCallbackQueue.Enqueue(callbackRequest);
            break;

        case ProbeWorkerInstruction::FAULTING_PROBE:
            m_pLogger->Log(LogLevel::Error, _LS("Function probe faulting in function: 0x%08x"), payload.functionId);
            callbackRequest.payload.functionId = payload.functionId;
            m_managedCallbackQueue.Enqueue(callbackRequest);
            break;

        case ProbeWorkerInstruction::UNINSTALL_PROBES:
            hr = UninstallProbes();
            if (hr != S_OK)
            {
                m_pLogger->Log(LogLevel::Error, _LS("Failed to uninstall probes: 0x%08x"), hr);
            }
            callbackRequest.payload.hr = hr;
            m_managedCallbackQueue.Enqueue(callbackRequest);
            break;

        default:
            m_pLogger->Log(LogLevel::Error, _LS("Unknown message"));
            break;
        }
    }
}

void ProbeInstrumentation::DisableIncomingRequests()
{
    g_probeManagementQueue.Complete();
}

void ProbeInstrumentation::ShutdownBackgroundService()
{
    DisableIncomingRequests();
    m_managedCallbackQueue.Complete();
    m_managedCallbackThread.join();
    m_probeManagementThread.join();
}

void STDMETHODCALLTYPE ProbeInstrumentation::OnFunctionProbeFault(ULONG64 uniquifier)
{
    PROBE_WORKER_PAYLOAD payload = {};
    payload.instruction = ProbeWorkerInstruction::FAULTING_PROBE;

    //
    // For now the uniquifier can only ever be the function's id.
    // If this changes in the future, add a new payload field.
    //
    payload.functionId = static_cast<FunctionID>(uniquifier);
    g_probeManagementQueue.Enqueue(payload);
}

STDAPI DLLEXPORT RequestFunctionProbeInstallation(
    ULONG64 functionIds[],
    ULONG32 count,
    PARAMETER_BOXING_INSTRUCTIONS boxingInstructions[],
    ULONG32 parameterCounts[])
{
    HRESULT hr;

    //
    // This method receives N (where n is "count") function IDs that probes should be installed into.
    //
    // Along with this, boxing instructions are provided for every parameter in every requested function,
    // and the number of parameters for each function can be found using parameterCounts.
    //
    // The boxing types are passed in as a flattened multidimensional array (boxingInstructions).
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
        if (UINT32_MAX - offset < parameterCounts[i])
        {
            return E_INVALIDARG;
        }

        vector<PARAMETER_BOXING_INSTRUCTIONS> instructions;
        instructions.reserve(parameterCounts[i]);
        for (ULONG32 j = 0; j < parameterCounts[i]; j++)
        {
            const ULONG32 boxingInstructionIndex = offset + j;
            if (boxingInstructions[boxingInstructionIndex].instructionType == InstructionType::TYPESPEC)
            {
                if (boxingInstructions[boxingInstructionIndex].signatureBufferPointer == nullptr ||
                    boxingInstructions[boxingInstructionIndex].signatureBufferLength == 0)
                {
                    return E_INVALIDARG;
                }
            }

            instructions.push_back(boxingInstructions[boxingInstructionIndex]);
        }
        offset += parameterCounts[i];

        UNPROCESSED_INSTRUMENTATION_REQUEST request;
        request.functionId = static_cast<FunctionID>(functionIds[i]);
        request.boxingInstructions = instructions;

        requests.push_back(request);
    }

    PROBE_WORKER_PAYLOAD payload = {};
    payload.instruction = ProbeWorkerInstruction::INSTALL_PROBES;
    payload.requests = requests;
    IfFailRet(g_probeManagementQueue.Enqueue(payload));

    END_NO_OOM_THROW_REGION;

    return S_OK;
}

STDAPI DLLEXPORT RequestFunctionProbeUninstallation()
{
    HRESULT hr;

    PROBE_WORKER_PAYLOAD payload = {};
    payload.instruction = ProbeWorkerInstruction::UNINSTALL_PROBES;
    IfFailRet(g_probeManagementQueue.Enqueue(payload));

    return S_OK;
}

STDAPI DLLEXPORT RequestFunctionProbeRegistration(ULONG64 enterProbeId)
{
    HRESULT hr;

    PROBE_WORKER_PAYLOAD payload = {};
    payload.instruction = ProbeWorkerInstruction::REGISTER_PROBE;
    payload.functionId = static_cast<FunctionID>(enterProbeId);
    IfFailRet(g_probeManagementQueue.Enqueue(payload));

    return S_OK;
}

bool ProbeInstrumentation::HasRegisteredProbe()
{
    return m_probeFunctionId != 0;
}

HRESULT ProbeInstrumentation::InstallProbes(vector<UNPROCESSED_INSTRUMENTATION_REQUEST>& requests)
{
    HRESULT hr;

    m_pLogger->Log(LogLevel::Debug, _LS("Installing function probes"));

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

        if (req.functionId == m_probeFunctionId)
        {
            return E_INVALIDARG;
        }

        // For now just use the function id as the uniquifier.
        // Consider allowing the caller to specify one.
        processedRequest.uniquifier = static_cast<ULONG64>(req.functionId);

        IfFailLogRet(m_pCorProfilerInfo->GetFunctionInfo2(
            req.functionId,
            NULL,
            nullptr,
            &processedRequest.moduleId,
            &processedRequest.methodDef,
            0,
            nullptr,
            nullptr));

        ComPtr<IMetaDataEmit> pMetadataEmit;
        IfFailRet(m_pCorProfilerInfo->GetModuleMetaData(
            processedRequest.moduleId,
            ofRead | ofWrite,
            IID_IMetaDataEmit,
            reinterpret_cast<IUnknown **>(&pMetadataEmit)));

        // Process the boxing instructions, converting any typespecs into metadata tokens.
        processedRequest.boxingInstructions.reserve(req.boxingInstructions.size());
        for (auto const& instructions : req.boxingInstructions)
        {
            PARAMETER_BOXING_INSTRUCTIONS newInstructions = {};
            if (instructions.instructionType == InstructionType::TYPESPEC)
            {
                newInstructions.instructionType = InstructionType::METADATA_TOKEN;
                IfFailRet(pMetadataEmit->GetTokenFromTypeSpec(
                    instructions.signatureBufferPointer,
                    instructions.signatureBufferLength,
                    &newInstructions.token.mdToken));
            }
            else
            {
                newInstructions = instructions;
            }

            processedRequest.boxingInstructions.push_back(newInstructions);
        }

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
        COR_PRF_REJIT_BLOCK_INLINING,
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

    m_pLogger->Log(LogLevel::Debug, _LS("Uninstalling function probes"));

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
    //
    // Workaround:
    // Enable COR_PRF_MONITOR_JIT_COMPILATION even though we don't need the callbacks.
    // It appears that without this flag set our RequestReJITWithInliners calls will sometimes
    // not actually trigger a rejit despite returning successfully.
    //
    // This issue most commonly occurs on MacOS.
    //
    eventsLow |= COR_PRF_MONITOR::COR_PRF_ENABLE_REJIT | COR_PRF_MONITOR::COR_PRF_MONITOR_JIT_COMPILATION;
}

HRESULT STDMETHODCALLTYPE ProbeInstrumentation::GetReJITParameters(ModuleID moduleId, mdMethodDef methodDef, ICorProfilerFunctionControl* pFunctionControl)
{
    HRESULT hr;

    if (m_pLogger->IsEnabled(LogLevel::Trace))
    {
        m_pLogger->Log(LogLevel::Trace, _LS("ReJIT - moduleId: 0x%08x, methodDef: 0x%04x"), moduleId, methodDef);
    }

    INSTRUMENTATION_REQUEST request;
    {
        lock_guard<mutex> lock(m_instrumentationProcessingMutex);
        auto const& it = m_activeInstrumentationRequests.find({moduleId, methodDef});
        if (it == m_activeInstrumentationRequests.end())
        {
            m_pLogger->Log(LogLevel::Debug, _LS("ReJIT cache miss - moduleId: 0x%08x, methodDef: 0x%04x"));
            return E_FAIL;
        }
        request = it->second;
    }

    hr = ProbeInjector::InstallProbe(
        m_pCorProfilerInfo,
        pFunctionControl,
        &OnFunctionProbeFault,
        request);

    if (FAILED(hr))
    {
        m_pLogger->Log(LogLevel::Error, _LS("Failed to install probes, reverting: 0x%08x"), hr);
        RequestFunctionProbeUninstallation();
        return hr;
    }

    return S_OK;
}

STDAPI DLLEXPORT RegisterFunctionProbeCallbacks(
    ProbeRegistrationCallback pRegistrationCallback,
    ProbeInstallationCallback pInstallationCallback,
    ProbeUninstallationCallback pUninstallationCallback,
    ProbeFaultCallback pFaultCallback)
{
    ExpectedPtr(pRegistrationCallback);
    ExpectedPtr(pInstallationCallback);
    ExpectedPtr(pUninstallationCallback);
    ExpectedPtr(pFaultCallback);

    //
    // Note: Require locking to access probe callbacks as it is
    // used on another thread (in ManagedCallbackThread).
    //
    // A lock-free approach could be used to safely update and observe the value of the callback,
    // however that would introduce the edge case where the provided callback is unregistered
    // right before it is invoked.
    // This means that the unregistered callback would still be invoked, leading to potential issues
    // such as calling into an instanced method that has been disposed.
    //
    // For simplicitly just use locking for now as it prevents the above edge case.
    //
    lock_guard<mutex> lock(g_probeManagementCallbacksMutex);

    // Just check one of the callbacks to see if they're already set,
    // a mixture of set and unset callbacks is not supported.
    if (g_probeManagementCallbacks.pProbeRegistrationCallback != nullptr)
    {
        return E_FAIL;
    }

    g_probeManagementCallbacks.pProbeRegistrationCallback = pRegistrationCallback;
    g_probeManagementCallbacks.pProbeInstallationCallback = pInstallationCallback;
    g_probeManagementCallbacks.pProbeUninstallationCallback = pUninstallationCallback;
    g_probeManagementCallbacks.pProbeFaultCallback = pFaultCallback;

    return S_OK;
}

STDAPI DLLEXPORT UnregisterFunctionProbeCallbacks()
{
    lock_guard<mutex> lock(g_probeManagementCallbacksMutex);
    g_probeManagementCallbacks = {};
    return S_OK;
}
