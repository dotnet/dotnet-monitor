@echo off
setlocal

set testAssembly=%1
set configuration=%2
set targetFramework=%3
set architecture=%~4
set timeoutMinutes=%~5

set filterArgs=
if not "%~6" == "" (
   set filterArgs=--filter ^"%~6^"
)

set exit_code=0

echo "Start tests..."

dotnet.exe test ^
  "%HELIX_CORRELATION_PAYLOAD%\%testAssembly%\%configuration%\%targetFramework%\%testAssembly%.dll" ^
  --logger:"console;verbosity=normal" ^
  --logger:"trx;LogFileName=%testAssembly%_%targetFramework%_%architecture%.trx" ^
  --logger:"html;LogFileName=%testAssembly%_%targetFramework%_%architecture%.html" ^
  --ResultsDirectory:%HELIX_WORKITEM_UPLOAD_ROOT% ^
  --blame "CollectHangDump;TestTimeout=%timeoutMinutes%m" ^
  %filterArgs%

if not errorlevel 0 (
    set exit_code=%errorlevel%
)

echo "Finished tests; exit code: %exit_code%"

exit /b %exit_code%