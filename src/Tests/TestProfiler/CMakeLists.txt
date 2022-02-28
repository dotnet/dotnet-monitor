cmake_minimum_required(VERSION 3.14)

project(TestProfiler)

if(CLR_CMAKE_HOST_WIN32)
    set(PLATFORM_SOURCES
        TestProfiler.def
        )
endif(CLR_CMAKE_HOST_WIN32)

set(SOURCES
    ${PLATFORM_SOURCES}
    ${PROFILER_SOURCES}
    EnvProfiler/EnvProfiler.cpp
    ClassFactory.cpp
    DllMain.cpp
    ProfilerBase.cpp
    )

add_library(TestProfiler SHARED ${SOURCES})

install(TARGETS TestProfiler)

if(CLR_CMAKE_HOST_WIN32)
    install(FILES $<TARGET_PDB_FILE:${PROJECT_NAME}> DESTINATION bin OPTIONAL)
endif(CLR_CMAKE_HOST_WIN32)