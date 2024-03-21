// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <vector>

enum class IpcCommand : unsigned short
{
    Unknown,
    Status,
    Callstack
};

struct IpcMessage
{
    short CommandSet;
    short Command;
    std::vector<BYTE> Payload;
};
