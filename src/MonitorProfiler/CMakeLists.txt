cmake_minimum_required(VERSION 3.14)

project(MonitorProfiler)

if(CLR_CMAKE_HOST_WIN32)
    set(PLATFORM_SOURCES
        MonitorProfiler.def
        )
endif(CLR_CMAKE_HOST_WIN32)

set(SOURCES
    ${PLATFORM_SOURCES}
    ${PROFILER_SOURCES}
    MainProfiler/MainProfiler.cpp
    ClassFactory.cpp
    DllMain.cpp
    ProfilerBase.cpp
    )

add_library(MonitorProfiler SHARED ${SOURCES})

install(TARGETS MonitorProfiler)

if(CLR_CMAKE_HOST_WIN32)
    install(FILES $<TARGET_PDB_FILE:${PROJECT_NAME}> DESTINATION bin OPTIONAL)
endif(CLR_CMAKE_HOST_WIN32)