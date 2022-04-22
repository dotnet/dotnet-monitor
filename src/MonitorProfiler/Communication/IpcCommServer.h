// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include <string>
#include <memory>
#include <atomic>

#include "SocketWrapper.h"
#include "IpcCommClient.h"

#if TARGET_UNIX
//This is actually not defined on Unix
#define UNIX_PATH_MAX 108
#endif

class IpcCommServer
{
public:
    IpcCommServer();
    HRESULT Bind(const std::string& rootAddress);
    HRESULT Accept(std::shared_ptr<IpcCommClient>& client);
    void Shutdown();
private:
    const int ReceiveTimeoutMilliseconds = 10000;
    const int AcceptTimeoutSeconds = 3;
    const int Backlog = 20;
    SocketWrapper _domainSocket = 0;
    std::atomic_bool _shutdown;
};