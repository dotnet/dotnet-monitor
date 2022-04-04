// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

// Stub file to allow PAL headers to be included without including platform specific implementations.
// Native code should call appropriate C++ standard APIs and drop down to platform APIs when necessary.

// PAL relies on safemath.h to bring in type_traits under C++ linkage
#ifdef PAL_STDCPP_COMPAT
#include <type_traits>
#endif
