cmake_minimum_required(VERSION 3.14)


add_subdirectory(Common)

include_directories(
    Common
    )

add_subdirectory(MonitorProfiler)
add_subdirectory(MutatingMonitorProfiler)
