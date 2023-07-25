cmake_minimum_required(VERSION 3.14)

project(MutatingMonitorProfiler)

if(CLR_CMAKE_HOST_WIN32)
    set(SOURCES
        ${SOURCES}
        MutatingMonitorProfiler.def
        )
    add_definitions(-DWIN32_LEAN_AND_MEAN)
endif(CLR_CMAKE_HOST_WIN32)

set(SOURCES
    ${SOURCES}
    ${PROFILER_SOURCES}
    MutatingMonitorProfiler.cpp
    ProbeInstrumentation/AssemblyProbePrep.cpp
    ProbeInstrumentation/ProbeInstrumentation.cpp
    ProbeInstrumentation/ProbeInjector.cpp
    Utilities/ILRewriter.cpp
    ClassFactory.cpp
    DllMain.cpp
    )

# Build library and split symbols
add_library_clr(MutatingMonitorProfiler SHARED ${SOURCES})
target_link_libraries(MutatingMonitorProfiler CommonMonitorProfiler)


if (CLR_CMAKE_HOST_UNIX)
    target_link_libraries(MutatingMonitorProfiler
    stdc++
    pthread)
endif(CLR_CMAKE_HOST_UNIX)

# Install library
install(TARGETS MutatingMonitorProfiler DESTINATION .)
# Install symbols
get_symbol_file_name(MutatingMonitorProfiler SymbolFileName)
install(FILES ${SymbolFileName} DESTINATION . OPTIONAL)
