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
    HRESULT ReceiveFixedBuffer(char* pBuffer, int bufferSize);
    HRESULT SendFixedBuffer(const char* pBuffer, int bufferSize);

private:
    SocketWrapper _socket;
    std::atomic_bool _shutdown;

private:
    static constexpr int MaxPayloadSize = 4 * 1024 * 1024; // 4 MiB
};
