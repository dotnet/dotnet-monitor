// Licensed to the.NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

template<typename T, typename U>
struct PairHash
{
    size_t operator()(const std::pair<T, U>& pair) const
    {
        std::hash<T> first;
        size_t firstResult = first(pair.first);

        std::hash<U> second;
        size_t secondResult = second(pair.second);

        //TODO Use a better hash merging algorithm
        return firstResult ^ secondResult;
    }
};