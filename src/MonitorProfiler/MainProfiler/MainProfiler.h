// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "../ProfilerBase.h"
#include "../Environment/Environment.h"
#include "../Logging/Logger.h"
#include "../Communication/CommandServer.h"
#include <memory>

class MainProfiler final :
    public ProfilerBase
{
private:
    std::shared_ptr<IEnvironment> m_pEnvironment;
    std::shared_ptr<ILogger> m_pLogger;

public:
    static GUID GetClsid();

    STDMETHOD(Initialize)(IUnknown* pICorProfilerInfoUnk) override;
    STDMETHOD(Shutdown)() override;
    STDMETHOD(LoadAsNotficationOnly)(BOOL *pbNotificationOnly) override;

private:
    HRESULT InitializeEnvironment();
    HRESULT InitializeLogging();
    HRESULT InitializeCommandServer();
    HRESULT MessageCallback(const IpcMessage& message);
private:
    std::unique_ptr<CommandServer> _commandServer;
};
