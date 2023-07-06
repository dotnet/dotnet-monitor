@echo off
setlocal

set testAssembly=%1
set configuration=%2
set targetFramework=%3
set architecture=%~4

set filterArgs=
if not "%~5" == "" (
   set filterArgs=--filter ^"%~5^"
)

set exit_code=0

echo "Start tests..."

dotnet.exe test ^
  "%HELIX_CORRELATION_PAYLOAD%\%testAssembly%\%configuration%\%targetFramework%\%testAssembly%.dll" ^
  --logger:"console;verbosity=normal" ^
  --logger:"trx;LogFileName=%testAssembly%_%targetFramework%_%architecture%.trx" ^
  --logger:"html;LogFileName=%testAssembly%_%targetFramework%_%architecture%.html" ^
  --ResultsDirectory:%HELIX_WORKITEM_UPLOAD_ROOT% ^
  --blame "CollectHangDump;TestTimeout=15m" ^
  %filterArgs%

if not errorlevel 0 (
    set exit_code=%errorlevel%
)

echo "Finished tests; exit code: %exit_code%"

exit /b %exit_code%