// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "cor.h"

typedef void (STDMETHODCALLTYPE *FaultingProbeCallback)(ULONG64);
constexpr COR_SIGNATURE FaultingProbeCallbackCorSignature [] = { IMAGE_CEE_CS_CALLCONV_STDCALL, 0x01, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I8 };
