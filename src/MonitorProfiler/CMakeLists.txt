cmake_minimum_required(VERSION 3.14)

project(MonitorProfiler)

if(CLR_CMAKE_HOST_WIN32)
    set(SOURCES
        ${SOURCES}
        Logging/DebugLogger.cpp
        MonitorProfiler.def
        )
endif(CLR_CMAKE_HOST_WIN32)

set(SOURCES
    ${SOURCES}
    ${PROFILER_SOURCES}
    Environment/EnvironmentHelper.cpp
    Environment/ProfilerEnvironment.cpp
    Logging/AggregateLogger.cpp
    MainProfiler/MainProfiler.cpp
    ClassFactory.cpp
    DllMain.cpp
    ProfilerBase.cpp
    )

# Build library and split symbols
add_library_clr(MonitorProfiler SHARED ${SOURCES})

# Install library
install(TARGETS MonitorProfiler DESTINATION .)
# Install symbols
get_symbol_file_name(MonitorProfiler SymbolFileName)
install(FILES ${SymbolFileName} DESTINATION . OPTIONAL)
