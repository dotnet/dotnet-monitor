// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "StartupEnvironmentUtilities.h"

#if !TARGET_WINDOWS
std::mutex StartupEnvironmentUtilities::s_getEnvMutex;
#endif