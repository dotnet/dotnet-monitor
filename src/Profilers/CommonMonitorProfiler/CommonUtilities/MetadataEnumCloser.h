// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "cor.h"
#include "corprof.h"

template<class T> class MetadataEnumCloser final
{
private:
    ComPtr<T> m_pEnumOwner;
    HCORENUM m_hEnum;

public:
    MetadataEnumCloser(T* pImport, HCORENUM hEnum) : m_pEnumOwner(pImport), m_hEnum(hEnum) {}
    ~MetadataEnumCloser()
    {
        m_pEnumOwner->CloseEnum(m_hEnum);
    }

    HCORENUM* GetEnumPtr() { return &m_hEnum; }
};
