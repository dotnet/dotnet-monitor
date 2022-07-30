cmake_minimum_required(VERSION 3.14)

project(MonitorProfiler)

if(CLR_CMAKE_HOST_WIN32)
    set(SOURCES
        ${SOURCES}
        Logging/DebugLogger.cpp
        MonitorProfiler.def
        )
    add_definitions(-DWIN32_LEAN_AND_MEAN)
endif(CLR_CMAKE_HOST_WIN32)

set(SOURCES
    ${SOURCES}
    ${PROFILER_SOURCES}
    Environment/EnvironmentHelper.cpp
    Environment/ProfilerEnvironment.cpp
    EventProvider/ProfilerEventProvider.cpp
    Logging/AggregateLogger.cpp
    Logging/Logger.cpp
    Logging/LoggerHelper.cpp
    Logging/NullLogger.cpp
    Logging/StdErrLogger.cpp
    MainProfiler/ExceptionTracker.cpp
    MainProfiler/MainProfiler.cpp
    MainProfiler/ThreadData.cpp
    MainProfiler/ThreadDataManager.cpp
    Stacks/StacksEventProvider.cpp
    Stacks/StackSampler.cpp
    Utilities/NameCache.cpp
    Utilities/TypeNameUtilities.cpp
    ClassFactory.cpp
    DllMain.cpp
    ProfilerBase.cpp
    Communication/IpcCommServer.cpp
    Communication/IpcCommClient.cpp
    Communication/CommandServer.cpp
    )

# Build library and split symbols
add_library_clr(MonitorProfiler SHARED ${SOURCES})

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
