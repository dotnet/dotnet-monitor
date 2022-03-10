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

# Build library and split symbols
add_library_clr(TestProfiler SHARED ${SOURCES})

# Install library
install(TARGETS TestProfiler DESTINATION .)
# Install symbols
get_symbol_file_name(TestProfiler SymbolFileName)
install(FILES ${SymbolFileName} DESTINATION . OPTIONAL)
