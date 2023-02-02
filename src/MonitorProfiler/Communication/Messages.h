// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

enum class MessageType : short
{
    OK,
    Error,
    Callstack
};

struct IpcMessage
{
    MessageType MessageType = MessageType::OK;
    int Parameters = 0;
};
