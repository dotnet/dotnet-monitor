@echo off
%~dp0dotnet.cmd test --no-build --no-restore --results-directory %~dp0artifacts\TestResults %*
exit /b %ErrorLevel%
