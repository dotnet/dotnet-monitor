// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <string>
#include <memory>
#include <atomic>

#include "SocketWrapper.h"
#include "IpcCommClient.h"
#include "Logging/Logger.h"

class IpcCommServer
{
public:
    IpcCommServer(const std::shared_ptr<ILogger>& logger);
    ~IpcCommServer();
    HRESULT Bind(const std::string& rootAddress);
    HRESULT Accept(std::shared_ptr<IpcCommClient>& client);
    void Shutdown();
private:
    const int ReceiveTimeoutMilliseconds = 10000;
    const int AcceptTimeoutSeconds = 3;
    const int Backlog = 20;
    std::string _rootAddress;
    SocketWrapper _domainSocket = 0;
    std::atomic_bool _shutdown;
    std::shared_ptr<ILogger> _logger;
};
