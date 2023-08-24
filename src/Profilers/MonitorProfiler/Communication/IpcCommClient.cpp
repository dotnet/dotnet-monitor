// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "IpcCommClient.h"
#include <memory>
#include "corhlpr.h"
#include "macros.h"
#include "assert.h"

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
    char headersBuffer[sizeof(IpcCommand) + sizeof(int)];
    IfFailRet(ReceiveFixedBuffer(
        headersBuffer,
        sizeof(headersBuffer)
    ));

    int bufferOffset = 0; 
    message.Command = *reinterpret_cast<IpcCommand*>(&headersBuffer[bufferOffset]);
    bufferOffset += sizeof(IpcCommand);

    int payloadSize = *reinterpret_cast<int*>(&headersBuffer[bufferOffset]);
    bufferOffset += sizeof(int);

    assert(bufferOffset == sizeof(headersBuffer));

    if (payloadSize < 0 || payloadSize > MaxPayloadSize)
    {
        return E_FAIL;
    }

    if (payloadSize != 0)
    {
        IfOomRetMem(message.Payload.resize(payloadSize));
        IfFailRet(ReceiveFixedBuffer(
            reinterpret_cast<char*>(message.Payload.data()),
            payloadSize
        ));
    }

    return S_OK;
}

HRESULT IpcCommClient::ReceiveFixedBuffer(char* pBuffer, int bufferSize)
{
    ExpectedPtr(pBuffer);

    if (bufferSize == 0)
    {
        return S_OK;
    }

    if (bufferSize < 0)
    {
        return E_FAIL;
    }

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

HRESULT IpcCommClient::Send(const IpcMessage& message)
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

    if (message.Payload.size() > MaxPayloadSize)
    {
        return E_FAIL;
    }

    char headersBuffer[sizeof(IpcCommand) + sizeof(int)];

    int bufferOffset = 0; 
    *reinterpret_cast<IpcCommand*>(&headersBuffer[bufferOffset]) = message.Command;
    bufferOffset += sizeof(IpcCommand);

    int payloadSize = static_cast<int>(message.Payload.size());
    *reinterpret_cast<int*>(&headersBuffer[bufferOffset]) = payloadSize;
    bufferOffset += sizeof(int);

    assert(bufferOffset == sizeof(headersBuffer));

    IfFailRet(SendFixedBuffer(
        headersBuffer,
        sizeof(headersBuffer)));

    IfFailRet(SendFixedBuffer(
        reinterpret_cast<const char*>(message.Payload.data()),
        payloadSize));

    return S_OK;
}

HRESULT IpcCommClient::SendFixedBuffer(const char* pBuffer, int bufferSize)
{
    ExpectedPtr(pBuffer);

    if (bufferSize == 0)
    {
        return S_OK;
    }

    if (bufferSize < 0)
    {
        return E_FAIL;
    }

    int sent = 0;
    int offset = 0;
    do
    {
        sent = send(_socket, &pBuffer[offset], bufferSize - offset, 0);

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
    } while (offset < bufferSize);

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
