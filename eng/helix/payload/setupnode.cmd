set PATH=%HELIX_CORRELATION_PAYLOAD%\nodejs\%1;%PATH%
call npm config set prefix %HELIX_WORKITEM_ROOT%\.npm
if %ERRORLEVEL% neq 0 exit %ERRORLEVEL%
set PATH=%HELIX_WORKITEM_ROOT%\.npm;%PATH%
