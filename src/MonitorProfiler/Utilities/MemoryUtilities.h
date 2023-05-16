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
        // Derived from https://github.com/dotnet/runtime/blob/6dd808ff7ae62512330d2f111eb1f60f1ae40125/src/coreclr/pal/src/safecrt/strncpy_s.cpp
        // The only functional difference is that in debug builds this modified version will not fill any remaining space in the dst buffer with 0xFE.
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