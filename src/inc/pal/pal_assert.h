// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

// Stub file to allow PAL headers to be included without including platform specific implementations.
// Native code should call appropriate C++ standard APIs and drop down to platform APIs when necessary.

#define _ASSERTE(e) ((void)0)

#ifndef assert
#define assert(e) _ASSERTE(e)
#endif
