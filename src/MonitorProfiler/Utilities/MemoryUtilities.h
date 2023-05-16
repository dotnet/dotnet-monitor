// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "cor.h"
#include <errno.h>

class MemoryUtilities
{
    public:
        static HRESULT Copy(void* dst, size_t sizeInBytes, const void* src, size_t count)
        {
            int result = memcpy_s(dst, sizeInBytes, src, count);
            if (result != 0)
            {
                return HRESULT_FROM_WIN32(result);
            }
            return S_OK;
        }

#if !TARGET_WINDOWS
    private:
        // Derived from https://github.com/dotnet/runtime/blob/8e472c8886c9a02326a5035fc4549717f70ab818/src/coreclr/pal/src/safecrt/memcpy_s.cpp
        static errno_t memcpy_s(void* dst, size_t sizeInBytes, const void* src, size_t count)
        {
            if (count == 0)
            {
                /* nothing to do */
                return 0;
            }

            /* validation section */
            if (dst == NULL)
            {
                return EINVAL;
            }

            if (src == NULL || sizeInBytes < count)
            {
                /* zeroes the destination buffer */
                memset(dst, 0, sizeInBytes);

                if (src == NULL)
                {
                    return EINVAL;
                }

                if (sizeInBytes < count)
                {
                    return ERANGE;
                }

                return EINVAL;
            }

            UINT_PTR x = (UINT_PTR)dst, y = (UINT_PTR)src;
            memcpy(dst, src, count);
            return 0;
        }
#endif /* _IMPLEMENT_STRNCPY_S */
};