// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#define BEGIN_COM_MAP(t) \
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void **ppvObject) override \
    { \
        if (ppvObject == nullptr) \
        { \
            return E_POINTER; \
        }

#define COM_INTERFACE_ENTRY(i) \
        if (riid == IID_##i) \
        { \
            *ppvObject = (i*)this; \
            this->AddRef(); \
            return S_OK; \
        }

#define END_COM_MAP() \
        *ppvObject = nullptr; \
        return E_NOINTERFACE; \
    }

template <class T>
class ComPtr
{
private:
    T* m_p;

public:
    ComPtr() : m_p(nullptr)
    {
    }

    ComPtr(T* p)
    {
        if (nullptr != p)
        {
            p->AddRef();
        }
        m_p = p;
    }

    ~ComPtr()
    {
        Release();
    }

    void Attach(T* p)
    {
        if (m_p)
        {
            m_p->Release();
        }
        m_p = p;
    }

    T* Detach()
    {
        T* p = m_p;
        m_p = nullptr;
        return p;
    }

    void Release()
    {
        if (nullptr != m_p)
        {
            m_p->Release();
            m_p = nullptr;
        }
    }

    T* operator=(T* p) throw()
    {
        if (m_p != p)
        {
            ComPtr(p).Swap(*this);
        }
        return *this;
    }

    T* operator->()
    {
        return m_p;
    }

    T** operator&()
    {
        return &m_p;
    }

    operator T*()
    {
        return m_p;
    }

private:
    void Swap(ComPtr<T>& other)
    {
        T* p = m_p;
        m_p = other.m_p;
        other.m_p = p;
    }
};
