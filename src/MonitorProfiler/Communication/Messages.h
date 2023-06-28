// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <vector>

enum class PayloadType : short
{
    None,
    Utf8Json
};

enum class MessageType : short
{
    Unknown,
    Status,
    Callstack
};

struct IpcMessage
{
    PayloadType PayloadType = PayloadType::None;
    MessageType MessageType = MessageType::Unknown;
    int Parameter; // Optional data when PayloadType::None, or the payload size for other types.
    std::vector<BYTE> Payload;
};
