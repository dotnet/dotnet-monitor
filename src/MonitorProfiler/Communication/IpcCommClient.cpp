// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "IpcCommClient.h"
#include <memory>
#include "corhlpr.h"
#include "macros.h"
#include "assert.h"

using namespace std;

HRESULT IpcCommClient::Receive(IpcMessage& message)
{
    HRESULT hr;

    if (_shutdown.load())
    {
        return E_UNEXPECTED;
    }
    if (!_socket.Valid())
    {
        return E_UNEXPECTED;
    }

    //CONSIDER It is generally more performant to read and buffer larger chunks, in this case we are not expecting very frequent communication.
    char headersBuffer[sizeof(MessageType) + sizeof(ProfilerCommand) + sizeof(int)];
    IfFailRet(ReadFixedBuffer(
        sizeof(headersBuffer),
        headersBuffer
    ));

    message.MessageType = *reinterpret_cast<MessageType*>(headersBuffer);
    message.ProfilerCommand = *reinterpret_cast<ProfilerCommand*>(&headersBuffer[sizeof(MessageType)]);
    int payloadSize = *reinterpret_cast<int*>(&headersBuffer[sizeof(MessageType) + sizeof(ProfilerCommand)]);

    IfOomRetMem(message.Payload.resize(payloadSize));

    IfFailRet(ReadFixedBuffer(
        payloadSize,
        (char*)message.Payload.data()
    ));

    return S_OK;
}

HRESULT IpcCommClient::ReadFixedBuffer(int bufferSize, char* pBuffer)
{
    int read = 0;
    int offset = 0;

    do
    {
        int read = recv(_socket, &pBuffer[offset], bufferSize - offset, 0);
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

    } while (offset < bufferSize);

    return S_OK;
}


HRESULT IpcCommClient::Send(const SimpleIpcMessage& message)
{
    if (_shutdown.load())
    {
        return E_UNEXPECTED;
    }
    if (!_socket.Valid())
    {
        return E_UNEXPECTED;
    }

    char buffer[sizeof(MessageType) + sizeof(ProfilerCommand) + sizeof(int) + sizeof(int)];

    int bufferOffset = 0; 
    *reinterpret_cast<MessageType*>(&buffer[bufferOffset]) = message.MessageType;
    bufferOffset += sizeof(MessageType);

    *reinterpret_cast<ProfilerCommand*>(&buffer[bufferOffset]) = message.ProfilerCommand;
    bufferOffset += sizeof(ProfilerCommand);

    *reinterpret_cast<int*>(&buffer[bufferOffset]) = sizeof(int);
    bufferOffset += sizeof(int);

    *reinterpret_cast<int*>(&buffer[bufferOffset]) = message.Parameters;
    bufferOffset += sizeof(int);

    assert(bufferOffset == sizeof(buffer));

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
