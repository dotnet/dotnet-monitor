
# How To Use Egress Extensions

Egress extensions for `dotnet-monitor` are accessible through one of the four following mechanisms (items in **bold** are for official `dotnet-monitor` extensions) :
- **Installed alongside `dotnet-monitor`**
- **Installed via `dotnet tool install`**
- **Manually installed via Nuget package**
- Manually installed from the extension's repository (note that each repo may have its own instructions)

## Connecting extensions to `dotnet-monitor`

`dotnet-monitor` searches for extensions in four locations (**make sure this is correct - it may change**):
- Alongside the executing `dotnet-monitor` assembly
- Shared Settings Path (**link to configuration.md**)
- User Settings Path (**link to configuration.md**)
- Dotnet Tools Path
  - On Windows, `%USERPROFILE%\.dotnet\Tools`
  - On *nix, `%XDG_CONFIG_HOME%/dotnet/tools`
  - If `$XDG_CONFIG_HOME` isn't defined, we fall back to `$HOME/.config/dotnet/tools`

By default, official `dotnet-monitor` extensions installed alongside `dotnet-monitor` (Azure Blob Storage) or via `dotnet tool install` will be ready-to-configure; this will not require the installation of additional tools or manually placing files in one of the aforementioned locations. Extensions installed manually or from third-party repositories can be placed in any of the aforementioned locations (**maybe include example showing this?**).

