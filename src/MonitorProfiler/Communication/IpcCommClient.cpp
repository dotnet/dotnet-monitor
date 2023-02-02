// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "IpcCommClient.h"
#include <memory>

HRESULT IpcCommClient::Receive(IpcMessage& message)
{
    if (_shutdown.load())
    {
        return E_UNEXPECTED;
    }
    if (!_socket.Valid())
    {
        return E_UNEXPECTED;
    }

    //CONSIDER It is generally more performant to read and buffer larger chunks, in this case we are not expecting very frequent communication.
    char buffer[sizeof(MessageType) + sizeof(int)];
    int read = 0;
    int offset = 0;

    do
    {
        int read = recv(_socket, &buffer[offset], sizeof(buffer) - offset, 0);
        if (read == 0)
        {
            return E_ABORT;
        }
        if (read < 0)
        {
#if TARGET_UNIX
            if (errno == EINTR)
            {
                //Signal can interrupt operations. Try again.
                continue;
            }
#endif
            return SocketWrapper::GetSocketError();
        }
        offset += read;

    } while (offset < sizeof(buffer));

    message.MessageType = *reinterpret_cast<MessageType*>(buffer);
    message.Parameters = *reinterpret_cast<int*>(&buffer[sizeof(MessageType)]);

    return S_OK;
}

HRESULT IpcCommClient::Send(const IpcMessage& message)
{
    if (_shutdown.load())
    {
        return E_UNEXPECTED;
    }
    if (!_socket.Valid())
    {
        return E_UNEXPECTED;
    }

    char buffer[sizeof(MessageType) + sizeof(int)];
    *reinterpret_cast<MessageType*>(buffer) = message.MessageType;
    *reinterpret_cast<int*>(&buffer[sizeof(MessageType)]) = message.Parameters;

    int sent = 0;
    int offset = 0;
    do
    {
        sent = send(_socket, &buffer[offset], sizeof(buffer) - offset, 0);

        if (sent == 0)
        {
            return E_ABORT;
        }
        if (sent < 0)
        {
#if TARGET_UNIX
            if (errno == EINTR)
            {
                //Signal can interrupt operations. Try again.
                continue;
            }
#endif
            return SocketWrapper::GetSocketError();
        }
        offset += sent;
    } while (offset < sizeof(buffer));

    return S_OK;
}

HRESULT IpcCommClient::Shutdown()
{
    _shutdown.store(true);
    int result = shutdown(_socket,
#if TARGET_WINDOWS
        SD_BOTH
#else
        SHUT_RDWR
#endif
    );

    if (result != 0)
    {
        return SocketWrapper::GetSocketError();
    }
    return S_OK;
}

IpcCommClient::IpcCommClient(SOCKET socket) : _socket(socket), _shutdown(false)
{
}
