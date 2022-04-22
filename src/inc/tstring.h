// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include <string>

#if HOST_UNIX

typedef std::u16string tstring;
#define _T(str) u##str

#else // HOST_UNIX

typedef std::wstring tstring;
#define _T(str) L##str

#endif // HOST_UNIX
