@ECHO OFF
SETLOCAL
SETLOCAL EnableDelayedExpansion

:: This command launches a Visual Studio solution with environment variables required to use a local version of the .NET Core SDK.

:: This tells .NET Core to use the same dotnet.exe that build scripts use
SET DOTNET_ROOT=%~dp0.dotnet
SET DOTNET_ROOT(x86)=%~dp0.dotnet\x86

:: This tells .NET Core not to go looking for .NET Core in other places
SET DOTNET_MULTILEVEL_LOOKUP=0

:: Put our local dotnet.exe on PATH first so Visual Studio knows which one to use
SET PATH=%DOTNET_ROOT%;%PATH%

SET sln=%~1

IF NOT EXIST "%DOTNET_ROOT%\dotnet.exe" (
    echo .NET Core has not yet been installed. Run `%~dp0restore.cmd` to install tools
    exit /b 1
)

IF "%sln%"=="" (
    SET sln=%~dp0dotnet-monitor.sln
    echo Automatically chose solution file !sln!
)

IF "%VSINSTALLDIR%" == "" (
    start "" "%sln%"
) else (
    "%VSINSTALLDIR%\Common7\IDE\devenv.com" "%sln%"
)
