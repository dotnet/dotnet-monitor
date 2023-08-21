// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "IpcCommClient.h"
#include "IpcCommServer.h"
#include "Logging/Logger.h"
#include "CommonUtilities/StringUtilities.h"

IpcCommServer::IpcCommServer(const std::shared_ptr<ILogger>& logger) : _shutdown(false), _logger(logger)
{
}

IpcCommServer::~IpcCommServer()
{
    //We explicitly run the destructor first so that it can call closesocket prior to deletion of the path.
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

    HRESULT hr;
    sockaddr_un address;
    memset(&address, 0, sizeof(address));

    if ((rootAddress.length() == 0) || (rootAddress.length() >= sizeof(address.sun_path)))
    {
        return E_INVALIDARG;
    }

    _rootAddress = rootAddress;

    address.sun_family = AF_UNIX;
    IfFailRet(StringUtilities::Copy(address.sun_path, rootAddress.c_str()));

    //We don't error check this on purpose
    std::remove(rootAddress.c_str());

    _domainSocket = socket(AF_UNIX, SOCK_STREAM, 0);
    if (!_domainSocket.Valid())
    {
        return SocketWrapper::GetSocketError();
    }
    
    IfFailRet(_domainSocket.SetBlocking(false));

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
#if TARGET_WINDOWS

        fd_set set;
        FD_ZERO(&set);
        FD_SET(_domainSocket, &set);

        TIMEVAL timeout;
        timeout.tv_sec = AcceptTimeoutSeconds;
        timeout.tv_usec = 0;
        result = select(0, &set, nullptr, nullptr, &timeout);
#else
        //select has limitations on Linux; any descriptor value over 1024 is ignored.

        pollfd set[1];
        set[0].fd = _domainSocket;
        set[0].events = POLLIN;
        set[0].revents = 0;

        result = poll(set, 1, AcceptTimeoutSeconds * 1000);
#endif
        if (_shutdown.load())
        {
            return E_ABORT;
        }

        //0 indicates timeout
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
    } while (result <= 0);

    SocketWrapper clientSocket = SocketWrapper(accept(_domainSocket, nullptr, nullptr));
    if (!clientSocket.Valid())
    {
        return SocketWrapper::GetSocketError();
    }

#if TARGET_WINDOWS
    DWORD receiveTimeout = ReceiveTimeoutMilliseconds;

    HRESULT hr;

    //Windows sockets inherit non-blocking
    IfFailRet(clientSocket.SetBlocking(true));

#else
    TIMEVAL receiveTimeout;
    receiveTimeout.tv_sec = ReceiveTimeoutMilliseconds / 1000;
    receiveTimeout.tv_usec = 0;
#endif

    if (setsockopt(clientSocket, SOL_SOCKET, SO_RCVTIMEO, reinterpret_cast<const char*>(&receiveTimeout), sizeof(receiveTimeout)) != 0)
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
