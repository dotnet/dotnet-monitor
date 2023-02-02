// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#if TARGET_WINDOWS
#include <WinSock2.h>
#include <afunix.h>
#else
#include <sys/socket.h>
#include <poll.h>
#include <sys/un.h>
#include <unistd.h>
#include <fcntl.h>
typedef int SOCKET;
typedef struct timeval TIMEVAL;
#endif

//Includes pal on Linux
#include <windows.h>

class SocketWrapper
{
private:
    SOCKET _socket;

public:
    SocketWrapper(SOCKET socket) : _socket(socket)
    {
    }

    SocketWrapper(SocketWrapper&& other)
    {
        _socket = other._socket;
        other._socket = 0;
    }

    operator SOCKET const ()
    {
        return _socket;
    }

    SOCKET Release()
    {
        SOCKET s = _socket;
        _socket = 0;
        return s;
    }

    HRESULT SetBlocking(bool blocking)
    {
#if TARGET_WINDOWS
        u_long blockParameter = blocking ? 0 : 1; //0 == blocking
        if (ioctlsocket(_socket, FIONBIO, &blockParameter) != 0)
        {
            return SocketWrapper::GetSocketError();
        }
#else
        int flags = fcntl(_socket, F_GETFD);
        if (flags < 0)
        {
            return SocketWrapper::GetSocketError();
        }
        if (blocking)
        {
            flags &= ~O_NONBLOCK;
        }
        else
        {
            flags |= O_NONBLOCK;
        }

        if (fcntl(_socket, F_SETFD, flags) != 0)
        {
            return SocketWrapper::GetSocketError();
        }
#endif

        return S_OK;
    }

    static HRESULT GetSocketError()
    {
#if TARGET_UNIX
        return HRESULT_FROM_WIN32(errno);
#else
        return HRESULT_FROM_WIN32(WSAGetLastError());
#endif
    }

    const SocketWrapper& operator = (SocketWrapper&& other)
    {
        if (&other == this)
        {
            return *this;
        }

        Close();
        _socket = other._socket;
        other._socket = 0;

        return *this;
    }

    const SocketWrapper& operator = (const SocketWrapper&) = delete;
    SocketWrapper(const SocketWrapper&) = delete;

    ~SocketWrapper()
    {
        Close();
    }

    bool Valid()
    {
#if TARGET_UNIX
        return _socket > 0;
#else
        return _socket != 0 && _socket != INVALID_SOCKET;
#endif
    }

private:
    void Close()
    {
        //Note this should be idempotent. The destructor may be called twice.
        if (Valid())
        {
#if TARGET_WINDOWS
            closesocket(_socket);
#else
            close(_socket);
#endif
            _socket = 0;
        }
    }
};
