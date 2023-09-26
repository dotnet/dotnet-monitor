#!/usr/bin/env bash
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

# Obtain the location of the bash script to figure out where the root of the repo is.
__RepoRootDir="$(cd "$(dirname "$0")"/..; pwd -P)"

__CreateArchives=0
__BuildType=Debug
__CMakeArgs=
__CommonMSBuildArgs=
__Compiler=clang
__CompilerMajorVersion=
__CompilerMinorVersion=
__CrossBuild=0
__ExtraCmakeArgs=
__HostArch=x64
__HostOS=Linux
__IsMSBuildOnNETCoreSupported=0
__ManagedBuild=1
__ManagedBuildArgs=
__NativeBuild=1
__NumProc=1
__PortableBuild=1
__RootBinDir="$__RepoRootDir"/artifacts
__RuntimeSourceFeed=
__RuntimeSourceFeedKey=
__SkipConfigure=0
__SkipGenerateVersion=0
__TargetOS=Linux
__Test=0
__TestGroup=All
__UnprocessedBuildArgs=

usage_list+=("-skipmanaged: do not build managed components.")
usage_list+=("-skipnative: do not build native components.")
usage_list+=("-test: run xunit tests")

handle_arguments() {

    lowerI="$(echo "$1" | tr "[:upper:]" "[:lower:]")"
    case "$lowerI" in
        architecture|-architecture|-a)
            __TargetArch="$(echo "$2" | tr "[:upper:]" "[:lower:]")"
            __ShiftArgs=1
            ;;

        -binarylog|-bl|-clean|-integrationtest|-pack|-performancetest|-pipelineslog|-pl|-preparemachine|-publish|-r|-rebuild|-restore|-sign)
            __ManagedBuildArgs="$__ManagedBuildArgs $1"
            ;;

        configuration|-configuration|-c)
            _type="$(echo "$2" | tr "[:upper:]" "[:lower:]")"
            if [[ "$_type" == "release" ]]; then
                __BuildType=Release
            elif [[ "$_type" = "checked" ]]; then
                __BuildType=Checked
            fi

            __ShiftArgs=1
            ;;

        -clean|-binarylog|-bl|-pipelineslog|-pl|-restore|-r|-rebuild|-pack|-integrationtest|-performancetest|-sign|-publish|-preparemachine)
            __ManagedBuildArgs="$__ManagedBuildArgs $1"
            ;;

        -runtimesourcefeed)
            __ManagedBuildArgs="$__ManagedBuildArgs /p:DotNetRuntimeSourceFeed=$2"
            __ShiftArgs=1
            ;;

        -runtimesourcefeedkey)
            __ManagedBuildArgs="$__ManagedBuildArgs /p:DotNetRuntimeSourceFeedKey=$2"
            __ShiftArgs=1
             ;;

        skipmanaged|-skipmanaged)
            __ManagedBuild=0
            ;;

        skipnative|-skipnative)
            __NativeBuild=0
            ;;

        test|-test)
            __Test=1
            ;;

        testgroup|-testgroup)
            __TestGroup=$2
            __ShiftArgs=1
            ;;

        archive|-archive)
            __CreateArchives=1
            ;;

        -warnaserror|-nodereuse)
            __ManagedBuildArgs="$__ManagedBuildArgs $1 $2"
            __ShiftArgs=1
            ;;

        *)
            __UnprocessedBuildArgs="$__UnprocessedBuildArgs $1"
            ;;
    esac
}

source "$__RepoRootDir"/eng/native/build-commons.sh

__LogsDir="$__RootBinDir/log/$__BuildType"
__ArtifactsIntermediatesDir="$__RootBinDir/obj"

if [[ "$__TargetArch" == "armel" ]]; then
    # Armel cross build is Tizen specific and does not support Portable RID build
    __PortableBuild=0
fi

#
# Initialize the target distro name
#

initTargetDistroRid

echo "RID: $__OutputRid"

__BinDir="$__RootBinDir/bin/$__OutputRid.$__BuildType"
__IntermediatesDir="$__ArtifactsIntermediatesDir/$__OutputRid.$__BuildType"
__CommonMSBuildArgs="/p:PackageRid=$__OutputRid"

# Specify path to be set for CMAKE_INSTALL_PREFIX.
# This is where all built libraries will copied to.
__CMakeBinDir="$__BinDir"
export __CMakeBinDir

mkdir -p "$__IntermediatesDir"
mkdir -p "$__LogsDir"
mkdir -p "$__CMakeBinDir"

__ExtraCmakeArgs="$__ExtraCmakeArgs -DCLR_MANAGED_BINARY_DIR=$__RootBinDir/bin -DCLR_BUILD_TYPE=$__BuildType"

# Specify path to be set for CMAKE_INSTALL_PREFIX.
# This is where all built native libraries will copied to.
export __CMakeBinDir="$__BinDir"

#
# Setup LLDB paths for native build
#

if [ "$__HostOS" == "OSX" ]; then
    export LLDB_H="$__RepoRootDir"/src/SOS/lldbplugin/swift-4.0
    export LLDB_LIB=$(xcode-select -p)/../SharedFrameworks/LLDB.framework/LLDB
    export LLDB_PATH=$(xcode-select -p)/usr/bin/lldb

    export MACOSX_DEPLOYMENT_TARGET=10.12

    if [ ! -f $LLDB_LIB ]; then
        echo "Cannot find the lldb library. Try installing Xcode."
        exit 1
    fi

    # Workaround bad python version in /usr/local/bin/python2.7 on lab machines
    export PATH=/usr/bin:$PATH
    which python
    python --version

    if [[ "$__TargetArch" == x64 ]]; then
        __ExtraCmakeArgs="-DCMAKE_OSX_ARCHITECTURES=\"x86_64\" $__ExtraCmakeArgs"
    elif [[ "$__TargetArch" == arm64 ]]; then
        __ExtraCmakeArgs="-DCMAKE_OSX_ARCHITECTURES=\"arm64\" $__ExtraCmakeArgs"
    else
        echo "Error: Unknown OSX architecture $__TargetArch."
        exit 1
    fi
fi

#
# Build native components
#
if [[ "$__NativeBuild" == 1 ]]; then
    echo Generating Version Header
    "$__RepoRootDir/eng/common/msbuild.sh" \
        "$__RepoRootDir"/eng/empty.csproj \
        /restore \
        /t:GenerateRuntimeVersionFile \
        /p:NativeVersionFile="$__versionSourceFile" \
        /p:RuntimeVersionFile="$runtimeVersionHeaderFile" \
        /bl:"$__LogDir$"/GenNativeVersion.binlog \
        /clp:nosummary

    if [ "$?" != 0 ]; then
        echo "Generate version header failed."
        exit 1
    fi

    set -o pipefail
    build_native "$__TargetOS" "$__TargetArch" "$__RepoRootDir" "$__IntermediatesDir" "install" "$__ExtraCmakeArgs" "dotnet-monitor component" | tee "$__LogsDir"/make.log
    exit_code="$?"
    set +o pipefail

    if [ $exit_code != 0 ]; then
        echo "Native build failed."
        exit 1
    fi
fi

#
# Managed build
#

if [[ "$__ManagedBuild" == 1 ]]; then
    echo "Commencing managed build for $__BuildType in $__RootBinDir/bin"
    "$__RepoRootDir/eng/common/build.sh" --build --configuration "$__BuildType" $__CommonMSBuildArgs $__ManagedBuildArgs $__ArcadeScriptArgs $__UnprocessedBuildArgs
    if [ "$?" != 0 ]; then
        exit 1
    fi
fi

#
# Archive build
#

if [[ "$__CreateArchives" == 1 ]]; then
    echo "Commencing archiving for $__BuildType in $__RootBinDir/bin"
    "$__RepoRootDir/eng/common/build.sh" \
      --build \
      --configuration "$__BuildType" \
      -nobl \
      /bl:"$__LogsDir"/Archive.binlog \
      /p:CreateArchives=true \
      $__CommonMSBuildArgs \
      $__ManagedBuildArgs \
      $__ArcadeScriptArgs \
      $__UnprocessedBuildArgs
    if [ "$?" != 0 ]; then
        exit 1
    fi
fi

#
# Run xunit tests
#

if [[ "$__Test" == 1 ]]; then
    "$__RepoRootDir/eng/common/build.sh" \
    --test \
    --configuration "$__BuildType" \
    /p:BuildArch="$__TargetArch" \
    -nobl \
    /bl:"$__LogsDir"/Test.binlog \
    /p:TestGroup="$__TestGroup" \
    $__CommonMSBuildArgs \
    $__ManagedBuildArgs \
    $__ArcadeScriptArgs \
    $__UnprocessedBuildArgs

    if [ $? != 0 ]; then
        exit 1
    fi
fi

echo "BUILD: Repo sucessfully built."
echo "BUILD: Product binaries are available at $__CMakeBinDir"
