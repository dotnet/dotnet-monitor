// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "cor.h"
#include "corprof.h"
#include "com.h"
#include "AssemblyProbePrep.h"
#include "ProbeInjector.h"
#include "../Logging/Logger.h"
#include "../Utilities/PairHash.h"
#include "../Utilities/BlockingQueue.h"

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
    INSTALL_PROBES,
    UNINSTALL_PROBES
};

typedef struct _PROBE_WORKER_PAYLOAD
{
    ProbeWorkerInstruction instruction;
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
        BlockingQueue<PROBE_WORKER_PAYLOAD> m_probeManagementQueue;
        std::unordered_map<std::pair<ModuleID, mdMethodDef>, INSTRUMENTATION_REQUEST, PairHash<ModuleID, mdMethodDef>> m_activeInstrumentationRequests;
        std::mutex m_requestProcessingMutex;

    private:
        void WorkerThread();
        HRESULT Enable(std::vector<UNPROCESSED_INSTRUMENTATION_REQUEST>& requests);
        HRESULT Disable();

    public:
        ProbeInstrumentation(
            const std::shared_ptr<ILogger>& logger,
            ICorProfilerInfo12* profilerInfo);

        HRESULT InitBackgroundService();
        void ShutdownBackgroundService();

        bool HasProbes();
        bool IsEnabled();

        HRESULT RegisterFunctionProbe(FunctionID enterProbeId);
        HRESULT RequestFunctionProbeShutdown();
        HRESULT RequestFunctionProbeInstallation(ULONG64 functionIds[], ULONG32 count, ULONG32 argumentBoxingTypes[], ULONG32 argumentCounts[]);

        void AddProfilerEventMask(DWORD& eventsLow);

        HRESULT STDMETHODCALLTYPE GetReJITParameters(ModuleID moduleId, mdMethodDef methodId, ICorProfilerFunctionControl* pFunctionControl);
};
