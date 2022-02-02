cmake_minimum_required(VERSION 3.6.2)

cmake_policy(SET CMP0042 NEW) # MACOSX_RPATH is enabled by default.

if (CMAKE_VERSION VERSION_GREATER 3.7 OR CMAKE_VERSION VERSION_EQUAL 3.7)
  cmake_policy(SET CMP0066 NEW) # Honor per-config flags in try_compile() source-file signature.
endif()
if (CMAKE_VERSION VERSION_GREATER 3.8 OR CMAKE_VERSION VERSION_EQUAL 3.8)
  cmake_policy(SET CMP0067 NEW) # Honor language standard in try_compile() source-file signature
endif()
if (CMAKE_VERSION VERSION_GREATER 3.15 OR CMAKE_VERSION VERSION_EQUAL 3.15)
  cmake_policy(SET CMP0091 NEW) # MSVC runtime library flags are selected by an abstraction.
endif()

# Set the project name
project(dotnet-monitor)

include(eng/native/configurepaths.cmake)
include(${CLR_ENG_NATIVE_DIR}/configurecompiler.cmake)

include_directories("${CLR_SRC_NATIVE_DIR}")

if(MSVC)
  set(CMAKE_CXX_STANDARD_LIBRARIES "") # do not link against standard win32 libs i.e. kernel32, uuid, user32, etc.
endif (MSVC)

# Set commonly used directory names
set(CLR_DIR ${CMAKE_CURRENT_SOURCE_DIR})
set(VM_DIR ${CMAKE_CURRENT_SOURCE_DIR}/vm)
set(GENERATED_INCLUDE_DIR ${CMAKE_CURRENT_BINARY_DIR}/inc)
set(GENERATED_EVENTING_DIR ${CMAKE_CURRENT_BINARY_DIR}/Eventing)
set(PAL_REDEFINES_FILE ${CMAKE_CURRENT_SOURCE_DIR}/dlls/mscordac/palredefines.S)

# Avoid logging when skipping up-to-date copies
set(CMAKE_INSTALL_MESSAGE LAZY)

set(CORECLR_SET_RPATH ON)
if(CORECLR_SET_RPATH)
    # Enable @rpath support for shared libraries.
    set(MACOSX_RPATH ON)
endif(CORECLR_SET_RPATH)

OPTION(CLR_CMAKE_ENABLE_CODE_COVERAGE "Enable code coverage" OFF)

#---------------------------------------------------
# Define sub-component targets for the build
#---------------------------------------------------
include(components.cmake)

#-------------------------
# Enable C++ EH with SEH
#-------------------------
if (MSVC)
  string(REPLACE "/EHsc" "" CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS}")
  add_compile_options($<$<COMPILE_LANGUAGE:CXX>:/EHa>) # enable C++ EH (w/ SEH exceptions)
endif()

#-----------------------------------------
# Add Projects
#     - project which require platform header not clr's
#     - do not depend on clr's compile definitions
#-----------------------------------------
if(CLR_CMAKE_HOST_UNIX)
    add_subdirectory(src/pal)
else(CLR_CMAKE_HOST_UNIX)
    if(CLR_CMAKE_TARGET_UNIX)
        add_subdirectory(src/pal/src/libunwind)
    endif(CLR_CMAKE_TARGET_UNIX)
endif(CLR_CMAKE_HOST_UNIX)

# Add this subdir. We install the headers for the jit.
add_subdirectory(src/pal/prebuilt/inc)

add_subdirectory(src/minipal)

#-------------------------------------
# Include directory directives
#-------------------------------------
# Include the basic prebuilt headers - required for getting fileversion resource details.
include_directories("src/pal/prebuilt/inc")
include_directories(${CLR_ARTIFACTS_OBJ_DIR})

# Above projects do not build with these compile options
# All of the compiler options are specified in file compileoptions.cmake
# Do not add any new options here. They should be added in compileoptions.cmake
if(CLR_CMAKE_HOST_WIN32)
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/Zl>) # omit default library name in .OBJ
endif(CLR_CMAKE_HOST_WIN32)

#--------------------------------
# Definition directives
#  - all clr specific compile definitions should be included in this file
#  - all clr specific feature variable should also be added in this file
#----------------------------------
include(clrdefinitions.cmake)

if (CLR_CMAKE_HOST_UNIX)
  include_directories("src/pal/inc")
  include_directories("src/pal/inc/rt")
  include_directories("src/pal/src/safecrt")
endif (CLR_CMAKE_HOST_UNIX)

#------------------------------
# Add Product Directories
#------------------------------
include_directories("src/inc")
include_directories("src/minipal")

if(CLR_CMAKE_HOST_UNIX)
  # Include the dummy c++ include files
  include_directories("src/pal/inc/rt/cpp")

  # This prevents inclusion of standard C compiler headers
  add_compile_options(-nostdinc)
endif(CLR_CMAKE_HOST_UNIX)

add_subdirectory(src/inc)

if(CLR_CMAKE_HOST_UNIX)
    add_subdirectory(src/palrt)
endif(CLR_CMAKE_HOST_UNIX)
