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
#include <future>
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
    class CallbackInfo
    {
        public:
            IpcMessage Message;
            std::shared_ptr<std::promise<HRESULT>> Promise;
    };

    void ListeningThread();
    void ProcessMessage(const IpcMessage& message, std::shared_ptr<IpcCommClient> client);
    void ProcessResetMessage(const IpcMessage& message, std::shared_ptr<IpcCommClient> client);
    bool IsControlCommand(const IpcMessage& message);

    template<typename TCommand>
    void CreateControlMessage(CommandSet commandSet, TCommand command, CallbackInfo& info)
    {
        info.Message.CommandSet = static_cast<unsigned short>(commandSet);
        info.Message.Command = static_cast<unsigned short>(command);
        // Currently the managed payload always uses json deserialization
        // The native payload ignores this
        info.Message.Payload = std::vector<BYTE>({ (BYTE)'{', (BYTE)'}' });
        info.Promise = std::make_shared<std::promise<HRESULT>>();
    }

    // Wrapper methods for sending and logging
    HRESULT ReceiveMessage(std::shared_ptr<IpcCommClient> client, IpcMessage& message);
    HRESULT SendMessage(std::shared_ptr<IpcCommClient> client, const IpcMessage& message);
    HRESULT Shutdown(std::shared_ptr<IpcCommClient> client);

    void ProcessingThread(BlockingQueue<CallbackInfo>& queue);

    std::atomic_bool _shutdown;

    std::function<HRESULT(const IpcMessage& message)> _callback;
    std::function<HRESULT(const IpcMessage& message)> _validateMessageCallback;
    std::function<HRESULT(unsigned short commandSet, bool& unmanagedOnly)> _unmanagedOnlyCallback;

    IpcCommServer _server;

    // We allocates two queues and two threads to process messages.
    // UnmanagedOnlyQueue is dedicated to ICorProfiler api calls that cannot be called on threads that have previously invoked managed code, such as StackSnapshot.
    // Other command sets such as StartupHook call managed code and therefore interfere with StackSnapshot calls.
    BlockingQueue<CallbackInfo> _clientQueue;
    BlockingQueue<CallbackInfo> _unmanagedOnlyQueue;

    std::shared_ptr<ILogger> _logger;

    std::thread _listeningThread;
    std::thread _clientThread;
    std::thread _unmanagedOnlyThread;

    ComPtr<ICorProfilerInfo12> _profilerInfo;
};
