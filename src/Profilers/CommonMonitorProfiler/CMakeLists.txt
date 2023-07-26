cmake_minimum_required(VERSION 3.14)

project(CommonMonitorProfiler)

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
    CommonUtilities/NameCache.cpp
    CommonUtilities/ThreadNameCache.cpp
    CommonUtilities/ThreadUtilities.cpp
    CommonUtilities/TypeNameUtilities.cpp
    Environment/EnvironmentHelper.cpp
    Environment/ProfilerEnvironment.cpp
    EventProvider/ProfilerEventProvider.cpp
    Logging/AggregateLogger.cpp
    Logging/Logger.cpp
    Logging/LoggerHelper.cpp
    Logging/NullLogger.cpp
    Logging/StdErrLogger.cpp
    ProfilerBase.cpp
    )

# Build library and split symbols
add_library_clr(CommonMonitorProfiler STATIC ${SOURCES})

if (CLR_CMAKE_HOST_UNIX)
    target_link_libraries(CommonMonitorProfiler
    stdc++
    pthread)
endif(CLR_CMAKE_HOST_UNIX)

# Install library
install(TARGETS CommonMonitorProfiler DESTINATION .)
# Install symbols
get_symbol_file_name(CommonMonitorProfiler SymbolFileName)
install(FILES ${SymbolFileName} DESTINATION . OPTIONAL)
