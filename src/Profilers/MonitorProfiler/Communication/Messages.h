// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <vector>

enum class ProfilerCommand : unsigned short
{
    Callstack
};

enum class DotnetMonitorCommand : unsigned short
{
    Status
};

enum class CommandSet : unsigned short
{
    DotnetMonitor,
    Profiler,
    ManagedInProc
};

struct IpcMessage
{
    short CommandSet;
    short Command;
    std::vector<BYTE> Payload;
};
