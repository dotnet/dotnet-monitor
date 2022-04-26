@echo off
setlocal

set "_commonArgs=-restore -ci -prepareMachine -verbosity minimal -configuration Release"
set "_logDir=%~dp0..\artifacts\log\Release\"

powershell -ExecutionPolicy ByPass -NoProfile -command "& """%~dp0common\Build.ps1""" %_commonArgs% -pack -sign -publish /p:NoBuild=true -noBl /bl:'%_logDir%PackSignPublish.binlog' %*"
exit /b %ERRORLEVEL%
