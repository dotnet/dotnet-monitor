# Clone, build and test the repo
------------------------------

To clone, build and test the repo on Windows:

```cmd
cd $HOME
git clone https://github.com/dotnet/dotnet-monitor
cd dotnet-monitor
.\Build.cmd
.\Test.cmd
```


On Linux and macOS:

```bash
cd $HOME
git clone https://github.com/dotnet/dotnet-monitor
cd dotnet-monitor
./build.sh
./test.sh
```

If you prefer to use *Visual Studio*, *Visual Studio Code*, or *Visual Studio for Mac*, you can open the `dotnet monitor` solution at the root of the repo.

# Building the self-contained tool

In addition to the default framework-dependent `dotnet-monitor` global tool (which requires a matching
.NET runtime to be installed on the host), the repo can also build a **self-contained** variant packaged
as the `dotnet-monitor-selfcontained` global tool. This variant bundles the .NET runtime, so it runs on a
machine without any .NET runtime or SDK installed. It is **single-file** and **fully trimmed**
(`TrimMode=full`), and the whole tool is runtime-independent: the host, the bundled egress extensions
(`AzureBlobStorage`, `S3Storage`), and the injected startup hook are all self-contained.

The self-contained build is opt-in behind a single MSBuild toggle,
`DotNetMonitorBuildSelfContainedTool=true`, and is produced with `dotnet pack`:

```cmd
.\.dotnet\dotnet.exe pack src\Tools\dotnet-monitor\dotnet-monitor.csproj -c Release /p:DotNetMonitorBuildSelfContainedTool=true
```

This produces three NuGet packages (because self-contained tools are RID-specific): a small base/wrapper
package `dotnet-monitor-selfcontained` plus one package per runtime identifier
(`dotnet-monitor-selfcontained.win-x64` and `dotnet-monitor-selfcontained.linux-x64`). Installing the
base package resolves and installs the correct RID-specific package automatically:

```cmd
dotnet tool install -g dotnet-monitor-selfcontained
```

Notes and limitations:

- Only `win-x64` and `linux-x64` are built. Other runtime identifiers (for example `linux-musl-x64`
  for Alpine, or `win-x86`) are not produced by the self-contained tool; use the default
  framework-dependent `dotnet-monitor` tool or the container image for those.
- Native profilers are loaded into the **target** process, so a self-contained x64 tool can only
  profile same-OS x64 targets. Profiling musl or x86 targets is not supported by the self-contained
  package.
- The default framework-dependent `dotnet-monitor` package is unaffected when the toggle is off, so both
  packages can be built and shipped side by side.

Because the self-contained tool is fully trimmed, reflection-dependent code must be preserved explicitly.
This is handled in the build and should be kept in mind when adding features:

- The assemblies whose members are reached by reflection (reflection-based JSON serialization,
  configuration binding, and `DataAnnotations` validation) are preserved whole via `TrimmerRootAssembly`
  items (see `SelfContainedTool.targets` for the host and `src/Extensions/SelfContainedExtension.targets`
  for the extensions). The reflection-heavy diagnostic libraries (`TraceEvent`, `EventPipe`,
  `FastSerialization`) and `System.ComponentModel.TypeConverter` (for `[Range(typeof(TimeSpan), ...)]`
  validation) are rooted there as well.
- Trim-analysis warnings are **not** blanket-suppressed: `SuppressTrimAnalysisWarnings` is left off so the
  build fails (under `TreatWarningsAsErrors`) if a new reflection dependency would actually be trimmed.
  Warnings in our own code are addressed with real attributes — `[DynamicallyAccessedMembers]` where a
  member can be preserved precisely, and narrowly-justified `[UnconditionalSuppressMessage]` (in the
  `GlobalSuppressions.SelfContainedTrim.cs` files) where the referenced type set is already rooted. These
  attributes are gated on the toggle so the shipping framework-dependent assemblies stay identical.
- If you add a feature that depends on reflection over a new assembly or type, root the relevant assembly
  in the appropriate `*.targets` and validate that the trimmed tool/extension still works at runtime — a
  clean build alone does not prove a reflection path survived trimming.

# Updating native build support

Part of the dotnet/runtime repo has been copied into this repo in order to facilitate building of native code. When needing to update the native build support, take a look at [runtime-version.txt](../src/external/runtime-version.txt) for what files should be synchronized from the dotnet/runtime repo. Synchronizing these files is currently done as a manual process. Update the version file with the new commit and file information if a new synchronization occurs.
