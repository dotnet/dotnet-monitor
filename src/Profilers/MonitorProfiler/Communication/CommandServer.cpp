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
    std::function<HRESULT(const IpcMessage& message)> validateMessageCallback)
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

    IfFailLogRet_(_logger, _server.Bind(path));
    _listeningThread = std::thread(&CommandServer::ListeningThread, this);
    _clientThread = std::thread(&CommandServer::ClientProcessingThread, this);
    return S_OK;
}

void CommandServer::Shutdown()
{
    bool shutdown = false;
    if (_shutdown.compare_exchange_strong(shutdown, true))
    {
        _clientQueue.Complete();
        _server.Shutdown();

        _listeningThread.join();
        _clientThread.join();
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
        hr = client->Receive(message);
        if (FAILED(hr))
        {
            _logger->Log(LogLevel::Error, _LS("Unexpected error when receiving data: 0x%08x"), hr);
            // Best-effort shutdown, ignore the result.
            client->Shutdown();
            continue;
        }

        bool doEnqueueMessage = true;
        hr = _validateMessageCallback(message);
        if (FAILED(hr))
        {
            _logger->Log(LogLevel::Error, _LS("Failed to validate message: 0x%08x"), hr);
            doEnqueueMessage = false;
        }

        *reinterpret_cast<HRESULT*>(response.Payload.data()) = hr;

        hr = client->Send(response);
        if (FAILED(hr))
        {
            _logger->Log(LogLevel::Error, _LS("Unexpected error when sending data: 0x%08x"), hr);
            doEnqueueMessage = false;
        }

        hr = client->Shutdown();
        if (FAILED(hr))
        {
            _logger->Log(LogLevel::Warning, _LS("Unexpected error during shutdown: 0x%08x"), hr);
            // Not fatal, keep processing the message
        }

        if (doEnqueueMessage)
        {
            _clientQueue.Enqueue(message);
        }
    }
}

void CommandServer::ClientProcessingThread()
{
    HRESULT hr = _profilerInfo->InitializeCurrentThread();

    if (FAILED(hr))
    {
        _logger->Log(LogLevel::Error, _LS("Unable to initialize thread: 0x%08x"), hr);
        return;
    }

    while (true)
    {
        IpcMessage message;
        hr = _clientQueue.BlockingDequeue(message);
        if (hr != S_OK)
        {
            //We are complete, discard all messages
            break;
        }
        hr = _callback(message);
        if (hr != S_OK)
        {
            _logger->Log(LogLevel::Warning, _LS("IpcMessage callback failed: 0x%08x"), hr);
        }
    }
}
