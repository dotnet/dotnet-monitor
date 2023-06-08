// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#define START_NO_OOM_THROW_REGION try {
#define END_NO_OOM_THROW_REGION } catch (const std::bad_alloc&) { return E_OUTOFMEMORY; }
#define ReturnHResultIfOom(exp) START_NO_OOM_THROW_REGION; exp; END_NO_OOM_THROW_REGION;