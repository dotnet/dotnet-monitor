// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "cor.h"
#include "corprof.h"
#include "com.h"
#include "AssemblyProbePrep.h"
#include "ProbeInjector.h"
#include "CallbackDefinitions.h"
#include "../Logging/Logger.h"
#include "../Utilities/PairHash.h"

#include <unordered_map>
#include <vector>
#include <memory>
#include <mutex>
#include <thread>

typedef struct _UNPROCESSED_INSTRUMENTATION_REQUEST
{
    FunctionID functionId;
    std::vector<ULONG32> boxingTypes;
} UNPROCESSED_INSTRUMENTATION_REQUEST;

enum class ProbeWorkerInstruction
{
    REGISTER_PROBE,
    INSTALL_PROBES,
    UNINSTALL_PROBES,
    FAULTING_PROBE
};

typedef struct _PROBE_WORKER_PAYLOAD
{
    ProbeWorkerInstruction instruction;

    // Optional instruction-specific fields
    FunctionID functionId;
    std::vector<UNPROCESSED_INSTRUMENTATION_REQUEST> requests;
} PROBE_WORKER_PAYLOAD;

class ProbeInstrumentation
{
    private:
        ICorProfilerInfo12* m_pCorProfilerInfo;
        std::shared_ptr<ILogger> m_pLogger;

        FunctionID m_probeFunctionId;
        std::unique_ptr<AssemblyProbePrep> m_pAssemblyProbePrep;

        /* Probe management */
        std::thread m_probeManagementThread;
        std::unordered_map<std::pair<ModuleID, mdMethodDef>, INSTRUMENTATION_REQUEST, PairHash<ModuleID, mdMethodDef>> m_activeInstrumentationRequests;
        std::mutex m_instrumentationProcessingMutex;
        std::mutex m_probePinningMutex;

    private:
        void WorkerThread();
        HRESULT RegisterFunctionProbe(FunctionID enterProbeId);
        HRESULT InstallProbes(std::vector<UNPROCESSED_INSTRUMENTATION_REQUEST>& requests);
        HRESULT UninstallProbes();
        bool HasRegisteredProbe();

    private:
        static void STDMETHODCALLTYPE OnFunctionProbeFault(ULONG64 uniquifier);

    public:
        ProbeInstrumentation(
            const std::shared_ptr<ILogger>& logger,
            ICorProfilerInfo12* profilerInfo);

        HRESULT InitBackgroundService();
        void ShutdownBackgroundService();

        bool AreProbesInstalled();

        void AddProfilerEventMask(DWORD& eventsLow);

        HRESULT STDMETHODCALLTYPE GetReJITParameters(ModuleID moduleId, mdMethodDef methodId, ICorProfilerFunctionControl* pFunctionControl);
};
