cmake_minimum_required(VERSION 3.14)

project(SharedMonitorProfiler)

if(CLR_CMAKE_HOST_WIN32)
    set(SOURCES
        ${SOURCES}
        Logging/DebugLogger.cpp
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
    Utilities/NameCache.cpp
    Utilities/ThreadNameCache.cpp
    Utilities/ThreadUtilities.cpp
    Utilities/TypeNameUtilities.cpp
    ProfilerBase.cpp
    )

# Build library and split symbols
add_library_clr(SharedMonitorProfiler STATIC ${SOURCES})

if (CLR_CMAKE_HOST_UNIX)
    target_link_libraries(SharedMonitorProfiler
    stdc++
    pthread)
endif(CLR_CMAKE_HOST_UNIX)

# Install library
install(TARGETS SharedMonitorProfiler DESTINATION .)
# Install symbols
get_symbol_file_name(SharedMonitorProfiler SymbolFileName)
install(FILES ${SymbolFileName} DESTINATION . OPTIONAL)
