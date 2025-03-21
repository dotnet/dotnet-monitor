#!/bin/sh

# Copyright (c) Microsoft. All rights reserved.

# Working dirctory to return to
__InitialCWD=$(pwd)

# Location of the script
__ScriptDirectory=

# VsDbg Meta Version. It could be something like 'latest', 'vs2022', 'vs2019', 'vsfm-8', 'vs2017u5', or a fully specified version.
__VsDbgMetaVersion=

# Install directory of the vsdbg relative to the script.
__InstallLocation=

# When SkipDownloads is set to true, no access to internet is made.
__SkipDownloads=false

# Launches VsDbg after downloading/upgrading.
__LaunchVsDbg=false

# Mode used to launch vsdbg.
__VsDbgMode=

# Removes existing installation of VsDbg in the Install Location.
__RemoveExistingOnUpgrade=false

# Internal, fully specified version of the VsDbg. Computed when the meta version is used.
__VsDbgVersion=

__ExactVsDbgVersionUsed=false

# RuntimeID of dotnet
__RuntimeID=

# Alternative location of installed debugger
__AltInstallLocation=

# Whether to use the alternate location version of the debugger. This is set after verifying the version is up-to-date.
__UseAltDebuggerLocation=false

# Flag to indicate that we will have to download the zip version and extract it as a zip.
__UseZip=false

# Flag to indicate that we support offline installation and will report with a structured error message.
__OfflineMode=false

GetVsDbgShDataPrefix="GetVsDbgShData:"

__VsdbgCompressedFile=

# Echo a message and exit with a failure
fail()
{
    echo 
    echo "$1"
    exit 1
}

# Gets the script directory
get_script_directory()
{
    scriptDirectory=$(dirname "$0")
    cd "$scriptDirectory" || fail "Command failed: 'cd \"$scriptDirectory\"'"
    __ScriptDirectory=$(pwd)
    cd "$__InitialCWD" || fail "Command failed: 'cd \"$__InitialCWD\"'"
}

print_help()
{
    echo 'GetVsDbg.sh [-usho] -v V [-l L] [-r R] [-d M] [-e E]'
    echo ''
    echo 'This script downloads and configures vsdbg, the Cross Platform .NET Debugger'
    echo '-u    Deletes the existing installation directory of the debugger before installing the current version.'
    echo '-s    Skips any steps which requires downloading from the internet.'
    echo '-d M  Launches debugger after the script completion. Where M is the mode, "mi" or "vscode"'
    echo '-h    Prints usage information.'
    echo '-v V  Version V can be "latest" or a version number such as 15.0.25930.0'
    echo '-l L  Location L where the debugger should be installed. Can be absolute or relative'
    echo '-r R  Debugger for the RuntimeID will be installed'
    echo '-a A  Specify a different alternate location that the debugger might already be installed.'
    echo '-o    Enables "Offline Mode" This enables structured output to use if the current machine does not have access to internet.'
    echo '-e E  E is the full path to the compressed package obtained from outside of the script.'
    echo ''
    echo 'For more information about using this script with Visual Studio Code see:'
    echo 'https://github.com/OmniSharp/omnisharp-vscode/wiki/Attaching-to-remote-processes'
    echo ''
    echo 'For more information about using this script with Visual Studio see:'
    echo 'https://github.com/Microsoft/MIEngine/wiki/Offroad-Debugging-of-.NET-Core-on-Linux---OSX-from-Visual-Studio'
    echo ''
    echo 'To report issues, see:'
    echo 'https://github.com/omnisharp/omnisharp-vscode/issues'
}

get_dotnet_runtime_id()
{
    if [ "$(uname)" = "Darwin" ]; then
        if [ "$(uname -m)" = "arm64" ]; then
            __RuntimeID=osx-arm64
        else
            __RuntimeID=osx-x64
        fi
    elif [ "$(uname -m)" = "x86_64" ]; then
        __RuntimeID=linux-x64
        if [ -e /etc/os-release ]; then
            # '.' is the same as 'source' but is POSIX compliant
            . /etc/os-release
            if [ "$ID" = "alpine" ]; then
                __RuntimeID=linux-musl-x64
            fi
        fi
    elif [ "$(uname -m)" = "armv7l" ]; then
        __RuntimeID=linux-arm
    elif [ "$(uname -m)" = "aarch64" ]; then
         __RuntimeID=linux-arm64
         if [ -e /etc/os-release ]; then
            # '.' is the same as 'source' but is POSIX compliant
            . /etc/os-release
            if [ "$ID" = "alpine" ]; then
                __RuntimeID=linux-musl-arm64
            # Check to see if we have dpkg to get the real architecture on debian based linux OS.
            elif hash dpkg 2>/dev/null; then
                # Raspbian 32-bit will return aarch64 in 'uname -m', but it can only use the linux-arm debugger
                if [ "$(dpkg --print-architecture)" = "armhf" ]; then
                    echo 'Info: Overriding Runtime ID from linux-arm64 to linux-arm'
                    __RuntimeID=linux-arm
                fi
            fi
        fi
    fi
}

remap_runtime_id()
{
    case "$__RuntimeID" in
        "debian.8-x64"|"rhel.7.2-x64"|"centos.7-x64"|"fedora.23-x64"|"opensuse.13.2-x64"|"ubuntu.14.04-x64"|"ubuntu.16.04-x64"|"ubuntu.16.10-x64"|"fedora.24-x64"|"opensuse.42.1-x64")
            __RuntimeID=linux-x64
            ;;
        *)
            ;;
    esac
}

# Parses and populates the arguments
parse_and_get_arguments()
{
    while getopts "v:l:r:d:a:suhoe:" opt; do
        case $opt in
            v)
                __VsDbgMetaVersion=$OPTARG;
                ;;
            l)
                __InstallLocation=$OPTARG
                # Subdirectories under '~/.vs-debugger' should always be safe to remove
                case $__InstallLocation in
                   "$HOME/.vs-debugger/"*)
                      __RemoveExistingOnUpgrade=true
                      ;;
                esac
                ;;
            u)
                __RemoveExistingOnUpgrade=true
                ;;
            s)
                __SkipDownloads=true
                ;;
            d)
                __LaunchVsDbg=true
                __VsDbgMode=$OPTARG
                ;;
            r)
                __RuntimeID=$OPTARG
                ;;
            a)
                __AltInstallLocation=$OPTARG
                ;;
            h)
                print_help
                exit 1
                ;;
            o)
                __OfflineMode=true
                ;;
            e)
                __VsdbgCompressedFile=$OPTARG

                # Convert to absolute path. If `cd` fails, `pwd` will not run and the subshell will exit with 1.
                __VsDbgCompressedFullPath=$(cd "$(dirname "$__VsdbgCompressedFile")" || fail "Command Failed: 'cd \"\$(dirname \"$__VsdbgCompressedFile\")\"'" ; pwd) 2>/dev/null

                # `cd` will fail within the subshell and exit with 1.
                if [ $? -eq 0 ]; then
                    __VsdbgCompressedFile="$__VsDbgCompressedFullPath/$(basename "$__VsdbgCompressedFile")"
                else
                    fail "ERROR: Failed to get the absolute path to $__VsdbgCompressedFile."
                fi

                # Test to see we got an existing file.
                if [ ! -f "$__VsdbgCompressedFile" ]; then
                    fail "ERROR: The compressed file path $__VsdbgCompressedFile does not exist."
                fi
                ;;
            \?)
                echo "ERROR: Invalid Option: -$OPTARG"
                print_help
                exit 1;
                ;;
            :)
                echo "ERROR: Option expected for -$OPTARG"
                print_help
                exit 1
                ;;
        esac
    done

    if [ -z "$__VsDbgMetaVersion" ]; then
        fail "ERROR: Version is not an optional parameter"
    fi

    case "$__VsDbgMetaVersion" in
        -*)
            fail "ERROR: Version should not start with hyphen"
            ;;
    esac
    
    case "$__AltInstallLocation" in
        -*)
            fail "ERROR: Alternate install location should not start with hyphen"
            ;;
    esac

    if [ -z "$__InstallLocation" ]; then
        fail "ERROR: Install location is not an optional parameter"
    fi

    case "$__InstallLocation" in
        -*)
            fail "ERROR: Install location should not start with hyphen"
            ;;
    esac

    if [ "$__RemoveExistingOnUpgrade" = true ]; then
        if [ "$__InstallLocation" = "$__ScriptDirectory" ]; then
            fail "ERROR: Cannot remove the directory which has the running script. InstallLocation: $__InstallLocation, ScriptDirectory: $__ScriptDirectory"
        fi

        if [ "$__InstallLocation" = "$HOME" ]; then
            fail "ERROR: Cannot remove home ( $HOME ) directory."
        fi
    fi
}

# Prints the arguments to stdout for the benefit of the user and does a quick sanity check.
print_arguments()
{
    echo "Using arguments"
    echo "    Version                    : '$__VsDbgMetaVersion'"
    echo "    Location                   : '$__InstallLocation'"
    echo "    SkipDownloads              : '$__SkipDownloads'"
    echo "    LaunchVsDbgAfter           : '$__LaunchVsDbg'"
    if [ "$__LaunchVsDbg" = true ]; then
        echo "        VsDbgMode              : '$__VsDbgMode'"
    fi
    echo "    RemoveExistingOnUpgrade    : '$__RemoveExistingOnUpgrade'"
    if [ "$__OfflineMode" = true ]; then
        echo "    OfflineMode                : '$__OfflineMode'"
    fi
    if [ -n "$__VsdbgCompressedFile" ]; then
        echo "    CompressedFileToExtract    : '$__VsdbgCompressedFile'"
    fi
}

# Prepares installation directory.
prepare_install_location()
{
    if [ -f "$__InstallLocation" ]; then
        fail "ERROR: Path '$__InstallLocation' points to a regular file and not a directory"
    elif [ ! -d "$__InstallLocation" ]; then
        echo 'Info: Creating install directory'
        if ! mkdir -p "$__InstallLocation"; then
            fail "ERROR: Unable to create install directory: '$__InstallLocation'"
        fi
    fi
}

# Checks if the debugger is already installed in the alternate location. If so, verify the version and if it matches, use it.
verify_and_use_alt_install_location()
{
    if [ -n "$__AltInstallLocation" ] && [ -d "$__AltInstallLocation" ]; then
        __AltSuccessFile="$__AltInstallLocation/success_version.txt"
        if [ -f "$__AltSuccessFile" ]; then
            __AltVersion=$(tr -cd '0-9.' < "$__AltSuccessFile")
            echo "Info: Existing debugger install found at $__AltInstallLocation'"
            echo "    Version                    : '$__AltVersion'"
            if [ "$__VsDbgVersion" = "$__AltVersion" ]; then
                __InstallLocation=$__AltInstallLocation
                __UseAltDebuggerLocation=true
                __SkipDownloads=true
                echo "Info: Using debugger found at '$__InstallLocation'"
            fi
        fi
    fi
}

# Converts relative location of the installation directory to absolute location.
convert_install_path_to_absolute()
{
    if [ -z "$__InstallLocation" ]; then
        __InstallLocation=$(pwd)
    else
        if [ ! -d "$__InstallLocation" ]; then
            prepare_install_location
        fi

        cd "$__InstallLocation" || fail "Command Failed: 'cd \"$__InstallLocation\""
        __InstallLocation=$(pwd)
        cd "$__InitialCWD" || fail "Command Failed: 'cd \"$__InitialCWD\"'"
    fi
}

# Computes the VSDBG version
set_vsdbg_version()
{
    # This case statement is done on the lower case version of version_string
    # Add new version constants here
    # 'latest' version may be updated
    # all other version contstants i.e. 'vs2017u1' or 'vs2017u5' may not be updated after they are finalized
    version_string="$(echo "$1" | awk '{print tolower($0)}')"
    case "$version_string" in
        latest)
            __VsDbgVersion=17.13.20213.2
            ;;
        vs2022)
            __VsDbgVersion=17.13.20213.2
            ;;
        vs2019)
            __VsDbgVersion=17.13.20213.2
            ;;
        vsfm-8)
            __VsDbgVersion=17.13.20213.2
            ;;
        vs2017u5)
            __VsDbgVersion=17.13.20213.2
            ;;
        vs2017u1)
            __VsDbgVersion="15.1.10630.1"
            __UseZip=true
            ;;
        [0-9]*)
            __VsDbgVersion=$1
            __ExactVsDbgVersionUsed=true

            # .tar.gz format is only avaliabe on versions higher than 16.5.20117
             __UseZip=$(echo "$__VsDbgVersion" | tr '-' '.' | awk '{split($0,a,"."); if (a[1] > 16 || (a[1] == 16 && a[2] > 5) || (a[1] == 16 && a[2] == 5 && a[3] > 20117)) {print "false"} else {print "true"};}')
            ;;
        *)
            fail "ERROR: '$1' does not look like a valid version number."
    esac
}

# Removes installation directory if remove option is specified.
process_removal()
{
    if [ "$__RemoveExistingOnUpgrade" = true ]; then

        echo "Info: Attempting to remove '$__InstallLocation'"

        if [ -d "$__InstallLocation" ]; then
            wcOutput=$(lsof "$__InstallLocation/vsdbg" | wc -l)

            if [ "$wcOutput" -gt 0 ]; then
                fail "ERROR: vsdbg is being used in location '$__InstallLocation'"
            fi

            if ! rm -rf "$__InstallLocation"; then
                fail "ERROR: files could not be removed from '$__InstallLocation'"
            fi
        fi
        echo "Info: Removed directory '$__InstallLocation'"
    else
        echo "Info: Cleaning up old files from '$__InstallLocation'."

        # Delete the old success.txt. This should always exist since we just read it.
        rm "$__InstallLocation/success.txt"
        
        # Old files can cause problems if the new vsdbg doesn't contain them. Remove
        # the files which may not exist in the new version.
        for i in "$__InstallLocation"/*.dll; do
            [ -f "$i" ] && rm "$i"
        done
        for i in "$__InstallLocation"/*.vsdconfig; do
            [ -f "$i" ] && rm "$i"
        done
        for i in "$__InstallLocation"/libSystem.*Native*.so; do
            [ -f "$i" ] && rm "$i"
        done
        for i in "$__InstallLocation"/libSystem.*Native*.dylib; do
            [ -f "$i" ] && rm "$i"
        done
    fi
}

# Checks if the existing copy is the latest version.
check_latest()
{
    __SuccessFile="$__InstallLocation/success.txt"
    if [ -f "$__SuccessFile" ]; then
        __LastInstalled=$(cat "$__SuccessFile")
        echo "Info: Last installed version of vsdbg is '$__LastInstalled'"
        if [ "$__VsDbgVersion" = "$__LastInstalled" ]; then
            __SkipDownloads=true
            echo "Info: VsDbg is up-to-date"
        else
            process_removal
        fi
    else
        echo "Info: Previous installation at '$__InstallLocation' not found"
    fi
}

check_internet_connection()
{
    if hash wget 2>/dev/null; then
        wget -q --spider "$1"
    elif hash curl 2>/dev/null; then
        curl -Is "$1" | head -1 | grep 200
    else
        if [ "$__OfflineMode" = true ]; then
            echo "${GetVsDbgShDataPrefix}URL=$url"
        fi
        fail "ERROR: Unable to find 'wget' or 'curl'. Install 'curl' or 'wget'. It is needed to download the vsdbg package."
    fi

    if [ $? -ne 0 ]; then
        if [ "$__OfflineMode" = true ]; then
            echo "${GetVsDbgShDataPrefix}URL=$url"
        fi
        fail "ERROR: No internet connection."
    fi
}

download()
{
    if [ "$__UseZip" = false ]; then
        vsdbgFileExtension=".tar.gz"
    else
        echo "Warning: Version '${__VsDbgMetaVersion}' is only avaliable in zip."
        vsdbgFileExtension=".zip"
    fi
    vsdbgCompressedFile="vsdbg-${__RuntimeID}${vsdbgFileExtension}"
    target="$(echo "${__VsDbgVersion}" | tr '.' '-')"
    url="https://vsdebugger-cyg0dxb6czfafzaz.b01.azurefd.net/vsdbg-${target}/${vsdbgCompressedFile}"

    check_internet_connection "$url"

    echo "Downloading ${url}"
    if hash wget 2>/dev/null; then
        wget -q "$url" -O "$vsdbgCompressedFile"
    elif hash curl 2>/dev/null; then
        curl -s "$url" -o "$vsdbgCompressedFile"
    fi

    if [ $? -ne  0 ]; then
        echo
        echo "ERROR: Could not download ${url}"
        exit 1;
    fi

    __VsdbgCompressedFile=$vsdbgCompressedFile
}

extract()
{
    errorMessage=
    if [ "$__UseZip" = false ]; then
        if ! hash tar 2>/dev/null; then
            errorMessage="ERROR: Failed to find 'tar'."
        elif ! tar -xzf "$__VsdbgCompressedFile"; then
            errorMessage="ERROR: Failed to extract vsdbg."
        fi
    else
        if ! hash unzip 2>/dev/null; then
            errorMessage="ERROR: Failed to find 'zip'."
        elif ! unzip -o -q "$__VsdbgCompressedFile"; then
            errorMessage="ERROR: Failed to unzip vsdbg."
        fi
    fi

    # If no errors, continue
    if [ -z "$errorMessage" ]; then
        chmod +x ./vsdbg
        # Check to see if vsdbg has execute permissions.
        if [ ! -x ./vsdbg ]; then
            errorMessage="ERROR: Failed to set executable permissions on vsdbg."
        fi
    fi

    rm "$__VsdbgCompressedFile"

    # Error occured, cleanup the compressed binary and exit with error.
    if [ -n "$errorMessage" ]; then
        fail "$errorMessage"
    fi
}

get_script_directory

if [ -z "$1" ]; then
    echo "ERROR: Missing arguments for GetVsDbg.sh"
    print_help
    exit 1
else
    parse_and_get_arguments "$@"
fi

set_vsdbg_version "$__VsDbgMetaVersion"

check_latest
# only try and use the alternate debugger location if the one in the default location is not adequate
if [ "$__SkipDownloads" = false ]; then
    verify_and_use_alt_install_location
fi

echo "Info: Using vsdbg version '$__VsDbgVersion'"
convert_install_path_to_absolute
print_arguments

# Shortcut if we are using Alternate Debugger Location
if [ "$__UseAltDebuggerLocation" = false ]; then
    if [ "$__SkipDownloads" = true ]; then
        echo "Info: Skipping downloads"
    else
        prepare_install_location
        cd "$__InstallLocation" || fail "Command failed: 'cd \"$__InstallLocation\"'"

        # For the rest of this script we can assume the working directory is the install path

        # Check to see if we already have a compressed file to extract, if not, we need to download it.
        if [ -z "$__VsdbgCompressedFile" ]; then
            if [ -z "$__RuntimeID" ]; then
                get_dotnet_runtime_id
            elif [ "$__ExactVsDbgVersionUsed" = "false" ]; then
                # Remap the old distro-specific runtime ids unless the caller specified an exact build number.
                # We don't do this in the exact build number case so that old builds can be used.
                remap_runtime_id
            fi

            echo "Info: Using Runtime ID '$__RuntimeID'"
            download
        fi

        extract

        echo "$__VsDbgVersion" > success.txt
        # per greggm, this 'cd' can fail sometimes and is to be expected.
        # shellcheck disable=SC2164
        cd "$__InitialCWD"
        echo "Info: Successfully installed vsdbg at '$__InstallLocation'"
    fi
fi

if [ "$__LaunchVsDbg" = true ]; then
    # Note: The following echo is a token to indicate the vsdbg is getting launched.
    # If you were to change or remove this echo make the necessary changes in the MIEngine
    echo "Info: Launching vsdbg"
    "$__InstallLocation/vsdbg" "--interpreter=$__VsDbgMode"
    exit $?
fi

exit 0
