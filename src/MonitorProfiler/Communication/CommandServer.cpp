// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "CommandServer.h"
#include <thread>
#include "../Logging/Logger.h"

CommandServer::CommandServer(const std::shared_ptr<ILogger>& logger, ICorProfilerInfo12* profilerInfo) :
    _shutdown(false),
    _logger(logger),
    _profilerInfo(profilerInfo)
{
}

HRESULT CommandServer::Start(const std::string& path, std::function<HRESULT(const IpcMessage& message)> callback)
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
            continue;
        }

        IpcMessage response;
        response.MessageType = SUCCEEDED(hr) ? MessageType::OK : MessageType::Error;
        response.Parameters = hr;

        hr = client->Send(response);
        if (FAILED(hr))
        {
            _logger->Log(LogLevel::Error, _LS("Unexpected error when sending data: 0x%08x"), hr);
            continue;
        }
        hr = client->Shutdown();
        if (FAILED(hr))
        {
            _logger->Log(LogLevel::Warning, _LS("Unexpected error during shutdown: 0x%08x"), hr);
        }

        _clientQueue.Enqueue(message);
    }
}

void CommandServer::ClientProcessingThread()
{
    HRESULT hr = _profilerInfo->InitializeCurrentThread();
    _logger->Log(LogLevel::Error, _LS("Unable to initialize thread: 0x%08x"), hr);

    if (FAILED(hr))
    {
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