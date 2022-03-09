cmake_minimum_required(VERSION 3.14)

project(dotnet-monitor)

include(eng/native/configurepaths.cmake)
include(${CLR_ENG_NATIVE_DIR}/configurecompiler.cmake)

include_directories(
    src/external/runtime/src/coreclr/pal/prebuilt/inc
    src/inc
    )

if(CLR_CMAKE_HOST_UNIX)
    include_directories(
        src/external/runtime/src/coreclr/pal/inc/rt
        src/external/runtime/src/coreclr/pal/inc
        src/external/runtime/src/coreclr/inc
        src/inc/pal
        )
    add_compile_options(
        -DPAL_STDCPP_COMPAT
        )
    if(NOT CLR_CMAKE_HOST_OSX)
        add_link_options(
            --no-undefined
            )
    endif(NOT CLR_CMAKE_HOST_OSX)
endif(CLR_CMAKE_HOST_UNIX)

set(PROFILER_SOURCES
    ../../external/runtime/src/coreclr/pal/prebuilt/idl/corprof_i.cpp
    )

add_subdirectory(src/Tests/TestProfiler)
