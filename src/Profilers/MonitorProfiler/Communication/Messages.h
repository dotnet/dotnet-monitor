// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <vector>

enum class ProfilerCommand : unsigned short
{
    Callstack
};

enum class ServerResponseCommand : unsigned short
{
    Status
};

//
// Kept in sync with src\Microsoft.Diagnostics.Monitoring.WebApi\ProfilerMessage.cs even though not all
// command sets will be used by the profiler.
//
enum class CommandSet : unsigned short
{
    ServerResponse,
    Profiler,
    StartupHook
};

struct IpcMessage
{
    unsigned short CommandSet;
    unsigned short Command;
    std::vector<BYTE> Payload;
};
