cmake_minimum_required(VERSION 3.14)


add_subdirectory(Shared)

include_directories(
    Shared
    )

add_subdirectory(MonitorProfiler)
add_subdirectory(MutatingMonitorProfiler)
