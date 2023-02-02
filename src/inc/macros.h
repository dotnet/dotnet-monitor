// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#ifndef ExpectedPtr
#define ExpectedPtr(ptr) { if (nullptr == ptr) return E_POINTER; }
#endif

#ifndef HRESULT_FROM_ERRNO
#define HRESULT_FROM_ERRNO(value) \
        MAKE_HRESULT(value == 0 ? SEVERITY_SUCCESS : SEVERITY_ERROR, FACILITY_NULL, HRESULT_CODE(value))
#endif

#ifndef E_NOT_SET
#define E_NOT_SET HRESULT_FROM_WIN32(1168L) //ERROR_NOT_FOUND
#endif
