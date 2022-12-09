// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "SocketWrapper.h"
#include "Messages.h"
#include <atomic>

class IpcCommClient
{
    friend class IpcCommServer;
public:
    HRESULT Receive(IpcMessage& message);
    HRESULT Send(const IpcMessage& message);
    HRESULT Shutdown();
    IpcCommClient(SOCKET socket);

private:
    SocketWrapper _socket;
    std::atomic_bool _shutdown;
};
