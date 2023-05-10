// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#if TARGET_WINDOWS
#include <processenv.h>
#include "tstring.h"
#else
#include <mutex>
#include <cstdlib>
#endif

//
// Utilities class to be used by initialization code before ProfilerEnvironment is able to be used.
//
class StartupEnvironmentUtilities
{
    private:
#if !TARGET_WINDOWS
        static std::mutex s_getEnvMutex;
#endif

    public:
        static HRESULT IsStartupSwitchSet(const char* name, BOOL& isSet)
        {
 #if TARGET_WINDOWS
            const WCHAR EnabledValue = L'1';

            isSet = FALSE;
            tstring tName = to_tstring(name);

            const DWORD bufferSize = 2;
            WCHAR buffer[bufferSize];

            DWORD retValue = GetEnvironmentVariableW(tName.c_str(), buffer, bufferSize);
            if (retValue == 0)
            {
                DWORD dwLastError = GetLastError();
                if (dwLastError == ERROR_ENVVAR_NOT_FOUND)
                {
                    return S_OK;
                }
                else
                {
                    return HRESULT_FROM_WIN32(dwLastError);
                }
            }
            else if (retValue != (bufferSize - 1)) // does not include null terminator 
            {
                return S_OK;
            }

            isSet = (buffer[0] == EnabledValue);
#else
            const char EnabledValue = '1';

            // Subsequent calls to getenv may invalidate the pointer returned by previous calls.
            std::lock_guard<std::mutex> lock(s_getEnvMutex);

            char *ret = std::getenv(name);
            isSet = (ret != nullptr && ret[0] == EnabledValue && ret[1] == '\0');
#endif

            return S_OK;
        }
};