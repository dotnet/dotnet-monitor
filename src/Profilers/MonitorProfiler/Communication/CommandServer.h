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
        std::function<HRESULT (const IpcMessage& message)> validateMessageCallback);
    void Shutdown();

private:
    void ListeningThread();
    void ClientProcessingThread();

    std::atomic_bool _shutdown;

    std::function<HRESULT(const IpcMessage& message)> _callback;
    std::function<HRESULT(const IpcMessage& message)> _validateMessageCallback;

    IpcCommServer _server;

    BlockingQueue<IpcMessage> _clientQueue;
    std::shared_ptr<ILogger> _logger;

    std::thread _listeningThread;
    std::thread _clientThread;

    ComPtr<ICorProfilerInfo12> _profilerInfo;
};
