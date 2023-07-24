call npm install -g azurite
if %ERRORLEVEL% neq 0 exit %ERRORLEVEL%
set TEST_AZURITE_MUST_INITIALIZE=1
