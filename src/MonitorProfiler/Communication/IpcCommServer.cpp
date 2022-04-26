// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "IpcCommClient.h"
#include "IpcCommServer.h"

IpcCommServer::IpcCommServer() : _shutdown(false)
{
}

IpcCommServer::~IpcCommServer()
{
    _domainSocket.~SocketWrapper();
    std::remove(_rootAddress.c_str());
}

HRESULT IpcCommServer::Bind(const std::string& rootAddress)
{
    if (_shutdown.load())
    {
        return E_UNEXPECTED;
    }

    if (_domainSocket.Valid())
    {
        return E_UNEXPECTED;
    }

    _rootAddress = rootAddress;

    sockaddr_un address;
    ZeroMemory(&address, sizeof(address));

    if ((rootAddress.length() == 0) || (rootAddress.length() >= sizeof(address.sun_path)))
    {
        return E_INVALIDARG;
    }

    //We don't error check this on purpose
    std::remove(rootAddress.c_str());

    address.sun_family = AF_UNIX;
#pragma warning(push)
#pragma warning(disable:4996)
    strncpy(address.sun_path, rootAddress.c_str(), sizeof(address.sun_path));
#pragma warning(pop)
    _domainSocket = socket(AF_UNIX, SOCK_STREAM, 0);
    if (!_domainSocket.Valid())
    {
        return SocketWrapper::GetSocketError();
    }
    if (bind(_domainSocket, reinterpret_cast<sockaddr*>(&address), sizeof(address)) != 0)
    {
        return SocketWrapper::GetSocketError();
    }
    if (listen(_domainSocket, Backlog) != 0)
    {
        return SocketWrapper::GetSocketError();
    }

    return S_OK;
}

HRESULT IpcCommServer::Accept(std::shared_ptr<IpcCommClient>& client)
{
    if (_shutdown.load())
    {
        return E_UNEXPECTED;
    }
    if (!_domainSocket.Valid())
    {
        return E_UNEXPECTED;
    }

    int result = 0;

    do
    {
        fd_set set;
        FD_ZERO(&set);
        FD_SET(_domainSocket, &set);

        TIMEVAL timeout;
        timeout.tv_sec = AcceptTimeoutSeconds;
        timeout.tv_usec = 0;
        result = select(2, &set, nullptr, nullptr, &timeout);

        if (_shutdown.load())
        {
            return E_ABORT;
        }
        if (result < 0)
        {
#if TARGET_UNIX
            if (errno == EINTR)
            {
                continue;
            }
#endif
            return SocketWrapper::GetSocketError();
        }
        if (result == 0)
        {
            //timeout
            continue;
        }
    } while (result <= 0);

    //TODO server socket should be in non-blocking mode

    SocketWrapper clientSocket = SocketWrapper(accept(_domainSocket, nullptr, nullptr));
    if (!clientSocket.Valid())
    {
        return SocketWrapper::GetSocketError();
    }
    if (setsockopt(clientSocket, SOL_SOCKET, SO_RCVTIMEO, reinterpret_cast<const char*>(&ReceiveTimeoutMilliseconds), sizeof(ReceiveTimeoutMilliseconds)) != 0)
    {
        return SocketWrapper::GetSocketError();
    }

    client = std::make_shared<IpcCommClient>(clientSocket.Release());

    return S_OK;
}

void IpcCommServer::Shutdown()
{
    _shutdown.store(true);
}
