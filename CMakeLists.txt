cmake_minimum_required(VERSION 3.14)

project(dotnet-monitor)

include(eng/native/configurepaths.cmake)
include(${CLR_ENG_NATIVE_DIR}/override/configurecompiler.cmake)

include_directories(
    src/external/runtime/src/coreclr/pal/prebuilt/inc
    src/inc
    ${CLR_ARTIFACTS_OBJ_DIR}
    )

include_directories(${PROJECT_BINARY_DIR})
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
    ${CMAKE_CURRENT_LIST_DIR}/src/external/runtime/src/coreclr/pal/prebuilt/idl/corprof_i.cpp
    )

# Add version files (non-Windows version embeddeding supported by add_library_clr function)
if(CLR_CMAKE_HOST_WIN32)
    set(SOURCES
        ${SOURCES}
        ${VERSION_FILE_RC_PATH}
        )
endif(CLR_CMAKE_HOST_WIN32)

add_subdirectory(src/Profilers)
