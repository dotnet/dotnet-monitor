cmake_minimum_required(VERSION 3.14)

project(MonitorProfiler)

if(CLR_CMAKE_HOST_WIN32)
    set(SOURCES
        ${SOURCES}
        MonitorProfiler.def
        )
    add_definitions(-DWIN32_LEAN_AND_MEAN)
endif(CLR_CMAKE_HOST_WIN32)

set(SOURCES
    ${SOURCES}
    ${PROFILER_SOURCES}
    MainProfiler/ExceptionTracker.cpp
    MainProfiler/MainProfiler.cpp
    MainProfiler/ThreadData.cpp
    MainProfiler/ThreadDataManager.cpp
    Stacks/StacksEventProvider.cpp
    Stacks/StackSampler.cpp
    ClassFactory.cpp
    DllMain.cpp
    Communication/IpcCommServer.cpp
    Communication/IpcCommClient.cpp
    Communication/CommandServer.cpp
    Communication/MessageCallbackManager.cpp
    )

# Include exceptions tracking feature
#add_compile_options(-DDOTNETMONITOR_FEATURE_EXCEPTIONS)

# Build library and split symbols
add_library_clr(MonitorProfiler SHARED ${SOURCES})
target_link_libraries(MonitorProfiler CommonMonitorProfiler)

if(CLR_CMAKE_HOST_WIN32)
    target_link_libraries(MonitorProfiler ws2_32)
endif(CLR_CMAKE_HOST_WIN32)

if (CLR_CMAKE_HOST_UNIX)
    target_link_libraries(MonitorProfiler
    stdc++
    pthread)
endif(CLR_CMAKE_HOST_UNIX)

# Install library
install(TARGETS MonitorProfiler DESTINATION .)
# Install symbols
get_symbol_file_name(MonitorProfiler SymbolFileName)
install(FILES ${SymbolFileName} DESTINATION . OPTIONAL)
