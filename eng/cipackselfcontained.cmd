@echo off
setlocal

REM Self-contained tool pack (dotnet-monitor-selfcontained).
REM
REM Produces the ADDITIONAL self-contained, single-file, fully-trimmed tool packages alongside the
REM framework-dependent dotnet-monitor package (which is produced by cipacksignpublish.cmd). Unlike that
REM script - which consumes prebuilt binaries via NoBuild=true - this one recompiles the self-contained
REM tool's closure with DotNetMonitorBuildSelfContainedTool=true so the trim/single-file annotations (only
REM compiled into the IL under that toggle) are present before full trimming runs. The matching-RID native
REM profilers are taken from the unified build output downloaded into artifacts\bin.
REM
REM Why a direct `dotnet pack` instead of Arcade's -pack: a self-contained tool packs as a RID-specific
REM tool, where the SDK fans the outer (RuntimeIdentifier-less) pack out into a base wrapper package plus one
REM package per RID (win-x64, linux-x64). Arcade's -build/-pack builds each ProjectToBuild with a single
REM concrete RID, which collapses that fan-out to one package. So the pack itself is run as a direct
REM `dotnet pack` on the host project; Arcade is still used to provision the toolset + restore (so the
REM internal runtime feeds configured by the surrounding pipeline steps are honored) and to sign the
REM produced packages.
REM
REM It deliberately does NOT run -publish: the signed packages are published as a pipeline artifact rather
REM than registered with BAR. BAR registration and darc-based release promotion of the self-contained
REM packages are a documented follow-up that must be validated in the official pipeline (see
REM documentation/building.md).
REM
REM NOTE: the signing step (Arcade -sign over the produced packages, including the single-file apphost and
REM the extension/native executables) can only be validated in the official pipeline; it cannot be exercised
REM locally. The pack mechanics (3-package fan-out, full trimming of host + extensions, package layout and the
REM _ValidateSelfContainedTool* guards) are validated by a direct `dotnet pack` locally.

set "_root=%~dp0..\"
set "_dotnet=%_root%.dotnet\dotnet.exe"
set "_logDir=%_root%artifacts\log\Release\"
set "_toggle=/p:DotNetMonitorBuildSelfContainedTool=true"
set "_commonArgs=-ci -prepareMachine -verbosity minimal -configuration Release"

REM Step 1: Provision the toolset (.dotnet, SignTool) and restore the self-contained closure. The closure is
REM scoped to the host + the two bundled extensions by src\Tools\dotnet-monitor\ProjectsToBuild.props
REM (imported by eng\Build.props only under the toggle).
powershell -ExecutionPolicy ByPass -NoProfile -command "& """%~dp0common\Build.ps1""" %_commonArgs% -restore %_toggle% -noBl /bl:'%_logDir%RestoreSelfContained.binlog' %*"
if errorlevel 1 exit /b %ERRORLEVEL%

REM Step 2: Provision the per-RID self-contained trim + runtime-pack assets for the closure. The host's
REM RID-agnostic restore (with the toggle) provisions both host RIDs + ILLink because the host sets
REM RuntimeIdentifiers + PublishTrimmed at project scope under the toggle. The extensions only set
REM PublishTrimmed in their publish-time properties, so a RID-agnostic restore omits the ILLink/runtime-pack
REM assets and the extensions would publish UNTRIMMED; restoring each extension once per RID with the
REM self-contained publish properties provisions them. (Arcade's -restore above also restores this closure,
REM but these explicit restores are what guarantee the trim assets the direct --no-restore pack depends on.)
"%_dotnet%" restore "%_root%src\Tools\dotnet-monitor\dotnet-monitor.csproj" %_toggle% %*
if errorlevel 1 exit /b %ERRORLEVEL%
"%_dotnet%" restore "%_root%src\Extensions\AzureBlobStorage\AzureBlobStorage.csproj" -r win-x64 %_toggle% /p:SelfContained=true /p:PublishTrimmed=true /p:PublishSingleFile=true %*
if errorlevel 1 exit /b %ERRORLEVEL%
"%_dotnet%" restore "%_root%src\Extensions\AzureBlobStorage\AzureBlobStorage.csproj" -r linux-x64 %_toggle% /p:SelfContained=true /p:PublishTrimmed=true /p:PublishSingleFile=true %*
if errorlevel 1 exit /b %ERRORLEVEL%
"%_dotnet%" restore "%_root%src\Extensions\S3Storage\S3Storage.csproj" -r win-x64 %_toggle% /p:SelfContained=true /p:PublishTrimmed=true /p:PublishSingleFile=true %*
if errorlevel 1 exit /b %ERRORLEVEL%
"%_dotnet%" restore "%_root%src\Extensions\S3Storage\S3Storage.csproj" -r linux-x64 %_toggle% /p:SelfContained=true /p:PublishTrimmed=true /p:PublishSingleFile=true %*
if errorlevel 1 exit /b %ERRORLEVEL%

REM Step 3: Pack via direct `dotnet pack` (RID fan-out -> base wrapper + per-RID self-contained, single-file,
REM fully-trimmed packages). --no-restore relies on the restores above. ValidateSelfContainedToolPackages
REM turns on the guard targets that assert the matching-RID native profilers are present and that only
REM dotnet-monitor-selfcontained* shipping packages are produced.
"%_dotnet%" pack "%_root%src\Tools\dotnet-monitor\dotnet-monitor.csproj" --configuration Release --no-restore %_toggle% /p:ValidateSelfContainedToolPackages=true "/bl:%_logDir%PackSelfContained.binlog" %*
if errorlevel 1 exit /b %ERRORLEVEL%

REM Step 4: Sign the produced packages (and their single-file apphost / native + extension executables) via
REM Arcade. Requires the Microbuild signing plugin enabled by the surrounding pipeline job; this is the step
REM that cannot be validated locally.
powershell -ExecutionPolicy ByPass -NoProfile -command "& """%~dp0common\Build.ps1""" %_commonArgs% -sign %_toggle% -noBl /bl:'%_logDir%SignSelfContained.binlog' %*"
exit /b %ERRORLEVEL%
