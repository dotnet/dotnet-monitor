# Storage Configuration

Some diagnostic features (e.g. memory dumps, stack traces) require that a directory is shared between the `dotnet monitor` tool and the target applications. The `Storage` configuration section allows specifying these directories to facilitate this sharing.

## Default Shared Path

First Available: 7.0

The default shared path option (`DefaultSharedPath`) can be set, which allows artifacts to be shared automatically without requiring additional configuration for each artifact type. By setting this property with an appropriate value, the following become available:
- dumps are temporarily stored in this directory or in a subdirectory.
- (8.0+) shared libraries are shared from `dotnet monitor` to target applications in this directory or in a subdirectory.
- (8.0+) in-process diagnostics share files back to `dotnet monitor` in this directory or in a subdirectory.

<details>
  <summary>JSON</summary>

  ```json
  {
    "Storage": {
      "DefaultSharedPath": "/diag"
    }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  Storage__DefaultSharedPath: "/diag"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_Storage__DefaultSharedPath
    value: "/diag"
  ```
</details>

## Dumps Path

Unlike the other diagnostic artifacts (for example, traces), memory dumps aren't streamed back from the target process to `dotnet monitor`. Instead, they are written directly to disk by the runtime. After successful collection of a process dump, `dotnet monitor` will read the process dump directly from disk. In the default configuration, the directory that the runtime writes its process dump to is the temp directory (`%TMP%` on Windows, `/tmp` on \*nix). It is possible to change to the ephemeral directory that these dump files get written to via the following configuration:

> [!NOTE]
> This option is optional if `dotnet monitor` is running in the same process namespace as the target processes or if `DefaultSharedPath` is specified.

<details>
  <summary>JSON</summary>

  ```json
  {
    "Storage": {
      "DumpTempFolder": "/diag/dumps/"
    }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  Storage__DumpTempFolder: "/diag/dumps/"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_Storage__DumpTempFolder
    value: "/diag/dumps/"
  ```
</details>

## Shared Library Path

First Available: 8.0 Preview 7

The shared library path option (`SharedLibraryPath`) allows specifying the path to where shared libraries are copied from the `dotnet monitor` installation to make them available to target applications for in-process diagnostics scenarios, such as call stack collection.

> [!NOTE]
> This option is not required if `DefaultSharedPath` is specified. This option provides an alternative directory path compared to the behavior of specifying `DefaultSharedPath`.

<details>
  <summary>JSON</summary>

  ```json
  {
    "Storage": {
      "SharedLibraryPath": "/diag/libs/"
    }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  Storage__SharedLibraryPath: "/diag/libs/"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_Storage__SharedLibraryPath
    value: "/diag/libs/"
  ```
</details>
