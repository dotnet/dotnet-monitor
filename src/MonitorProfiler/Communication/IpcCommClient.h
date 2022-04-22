// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    void Shutdown();
    IpcCommClient(SOCKET socket);

private:
    SocketWrapper _socket;
    std::atomic_bool _shutdown = false;
};
