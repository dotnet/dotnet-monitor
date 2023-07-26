cmake_minimum_required(VERSION 3.14)


add_subdirectory(CommonMonitorProfiler)

include_directories(
    CommonMonitorProfiler
    )

add_subdirectory(MonitorProfiler)
add_subdirectory(MutatingMonitorProfiler)
