// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "IpcCommServer.h"
#include "Messages.h"
#include "cor.h"
#include "corprof.h"
#include "com.h"
#include <functional>
#include <string>
#include <atomic>
#include <thread>
#include "Logging/Logger.h"
#include "CommonUtilities/BlockingQueue.h"

class CommandServer final
{
public:
    CommandServer(const std::shared_ptr<ILogger>& logger, ICorProfilerInfo12* profilerInfo);
    HRESULT Start(
        const std::string& path,
        std::function<HRESULT (const IpcMessage& message)> callback,
        std::function<HRESULT (const IpcMessage& message)> validateMessageCallback,
        std::function<HRESULT (unsigned short commandSet, bool& unmanagedOnly)> unmanagedOnlyCallback);
    void Shutdown();

private:
    void ListeningThread();
    void ClientProcessingThread();
    void UnmanagedOnlyProcessingThread();

    std::atomic_bool _shutdown;

    std::function<HRESULT(const IpcMessage& message)> _callback;
    std::function<HRESULT(const IpcMessage& message)> _validateMessageCallback;
    std::function<HRESULT(unsigned short commandSet, bool& unmanagedOnly)> _unmanagedOnlyCallback;

    IpcCommServer _server;

    // We allocates two queues and two threads to process messages.
    // UnmanagedOnlyQueue is dedicated to ICorProfiler api calls that cannot be called on threads that have previously invoked managed code, such as StackSnapshot.
    // Other command sets such as StartupHook call managed code and therefore interfere with StackSnapshot calls.
    BlockingQueue<IpcMessage> _clientQueue;
    BlockingQueue<IpcMessage> _unmanagedOnlyQueue;

    std::shared_ptr<ILogger> _logger;

    std::thread _listeningThread;
    std::thread _clientThread;
    std::thread _unmanagedOnlyThread;

    ComPtr<ICorProfilerInfo12> _profilerInfo;
};
