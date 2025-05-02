// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "CommandServer.h"
#include <thread>
#include "Logging/Logger.h"

CommandServer::CommandServer(const std::shared_ptr<ILogger>& logger, ICorProfilerInfo12* profilerInfo) :
    _shutdown(false),
    _server(logger),
    _logger(logger),
    _profilerInfo(profilerInfo)
{
}

HRESULT CommandServer::Start(
    const std::string& path,
    std::function<HRESULT(const IpcMessage& message)> callback,
    std::function<HRESULT(const IpcMessage& message)> validateMessageCallback,
    std::function<HRESULT(unsigned short commandSet, bool& unmanagedOnly)> unmanagedOnlyCallback)
{
    if (_shutdown.load())
    {
        return E_UNEXPECTED;
    }

    HRESULT hr;
#if TARGET_WINDOWS
    WSADATA wsaData;
    int result = WSAStartup(MAKEWORD(2, 2), &wsaData);
    if (result != 0)
    {
        return HRESULT_FROM_WIN32(result);
    }
#endif

    _callback = callback;
    _validateMessageCallback = validateMessageCallback;
    _unmanagedOnlyCallback = unmanagedOnlyCallback;

    IfFailLogRet_(_logger, _server.Bind(path));
    _listeningThread = std::thread(&CommandServer::ListeningThread, this);
    _clientThread = std::thread(&CommandServer::ProcessingThread, this, std::ref(_clientQueue));
    _unmanagedOnlyThread = std::thread(&CommandServer::ProcessingThread, this, std::ref(_unmanagedOnlyQueue));
    return S_OK;
}

void CommandServer::Shutdown()
{
    bool shutdown = false;
    if (_shutdown.compare_exchange_strong(shutdown, true))
    {
        _clientQueue.Complete();
        _unmanagedOnlyQueue.Complete();
        _server.Shutdown();

        _listeningThread.join();
        _clientThread.join();
        _unmanagedOnlyThread.join();
    }
}

void CommandServer::ListeningThread()
{
    // TODO: Handle oom scenarios
    IpcMessage response;
    response.CommandSet = static_cast<unsigned short>(CommandSet::ServerResponse);
    response.Command = static_cast<unsigned short>(ServerResponseCommand::Status);
    response.Payload.resize(sizeof(HRESULT));

    while (true)
    {
        std::shared_ptr<IpcCommClient> client;
        HRESULT hr = _server.Accept(client);
        if (FAILED(hr))
        {
            break;
        }

        IpcMessage message;

        //Note this can timeout if the client doesn't send anything
        hr = ReceiveMessage(client, message);
        if (FAILED(hr))
        {
            // Best-effort shutdown, ignore the result.
            client->Shutdown();
            continue;
        }

        bool doEnqueueMessage = true;
        hr = _validateMessageCallback(message);
        if (FAILED(hr))
        {
            _logger->Log(LogLevel::Error, _LS("Failed to validate message: 0x%08x"), hr);
            *reinterpret_cast<HRESULT*>(response.Payload.data()) = hr;
            SendMessage(client, response);
            Shutdown(client);
            continue;
        }

        if (!IsControlCommand(message))
        {
            ProcessMessage(message, client);
        }
        else
        {
            ProcessResetMessage(message, client);
        }
    }
}

void CommandServer::ProcessMessage(const IpcMessage& message, std::shared_ptr<IpcCommClient> client)
{
    IpcMessage response;
    response.CommandSet = static_cast<unsigned short>(CommandSet::ServerResponse);
    response.Command = static_cast<unsigned short>(ServerResponseCommand::Status);
    response.Payload.resize(sizeof(HRESULT));

    *reinterpret_cast<HRESULT*>(response.Payload.data()) = S_OK;
    SendMessage(client, response);
    Shutdown(client);

    CallbackInfo info;
    info.Message = message;
    bool unmanagedOnly = false;
    if (SUCCEEDED(_unmanagedOnlyCallback(info.Message.CommandSet, unmanagedOnly)) && unmanagedOnly)
    {
        _unmanagedOnlyQueue.Enqueue(info);
    }
    else
    {
        _clientQueue.Enqueue(info);
    }
}

void CommandServer::ProcessResetMessage(const IpcMessage& message, std::shared_ptr<IpcCommClient> client)
{
    IpcMessage response;
    response.CommandSet = static_cast<unsigned short>(CommandSet::ServerResponse);
    response.Command = static_cast<unsigned short>(ServerResponseCommand::Status);
    response.Payload.resize(sizeof(HRESULT));

    HRESULT hr = S_OK;

    if (message.CommandSet == static_cast<unsigned short>(CommandSet::Profiler))
    {
        CallbackInfo nativeCallbackInfo;

        CreateControlMessage(CommandSet::Profiler, message.Command, nativeCallbackInfo);

        _unmanagedOnlyQueue.Enqueue(nativeCallbackInfo);

        hr = nativeCallbackInfo.CompletionPromise->get_future().get();
    }
    if (message.CommandSet == static_cast<unsigned short>(CommandSet::StartupHook))
    {
        CallbackInfo managedCallbackInfo;

        CreateControlMessage(CommandSet::StartupHook, message.Command, managedCallbackInfo);

        _clientQueue.Enqueue(managedCallbackInfo);

        hr = managedCallbackInfo.CompletionPromise->get_future().get();
    }

    *reinterpret_cast<HRESULT*>(response.Payload.data()) = hr;
    SendMessage(client, response);
    Shutdown(client);
}

bool CommandServer::IsControlCommand(const IpcMessage& message)
{
    switch (message.CommandSet) {
        case static_cast<int>(CommandSet::Profiler):
            switch (message.Command)
            {
                case static_cast<int>(ProfilerCommand::StartAllFeatures):
                case static_cast<int>(ProfilerCommand::StopAllFeatures):
                    return true;
                default:
                    return false;
            }
        case static_cast<int>(CommandSet::StartupHook):
            switch (message.Command) {
                case static_cast<int>(StartupHookCommand::StartAllFeatures):
                case static_cast<int>(StartupHookCommand::StopAllFeatures):
                    return true;
                default:
                    return false;
            }
        default:
            return false;
    }
}

HRESULT CommandServer::ReceiveMessage(std::shared_ptr<IpcCommClient> client, IpcMessage& message)
{
    HRESULT hr;
    IfFailLogRet_(_logger, client->Receive(message));
    return S_OK;
}

HRESULT CommandServer::SendMessage(std::shared_ptr<IpcCommClient> client, const IpcMessage& message)
{
    HRESULT hr;
    IfFailLogRet_(_logger, client->Send(message));
    return S_OK;
}

HRESULT CommandServer::Shutdown(std::shared_ptr<IpcCommClient> client)
{
    HRESULT hr = client->Shutdown();
    if (FAILED(hr))
    {
        _logger->Log(LogLevel::Warning, _LS("Unexpected error during shutdown: 0x%08x"), hr);
    }
    return hr;
}

void CommandServer::ProcessingThread(BlockingQueue<CallbackInfo>& queue)
{
    HRESULT hr = _profilerInfo->InitializeCurrentThread();

    if (FAILED(hr))
    {
        _logger->Log(LogLevel::Error, _LS("Unable to initialize thread: 0x%08x"), hr);
        return;
    }

    while (true)
    {
        CallbackInfo info;
        hr = queue.BlockingDequeue(info);
        if (hr != S_OK)
        {
            //We are complete, discard all messages
            break;
        }

        hr = _callback(info.Message);
        if (hr != S_OK)
        {
            _logger->Log(LogLevel::Warning, _LS("IpcMessage callback failed: 0x%08x"), hr);
        }

        if (info.CompletionPromise)
        {
            info.CompletionPromise->set_value(hr);
        }
    }
}
