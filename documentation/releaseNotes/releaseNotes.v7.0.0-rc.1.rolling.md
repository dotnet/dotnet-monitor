Today we are releasing the next official preview of the `dotnet monitor` tool. This release includes:

- With this release, support for experimental features is added. Users can now enable an experimental feature through configuration flags when launching `dotnet monitor` to enable features that are still under development and not yet fully production-ready. (#2506)
- 🔬 Add collect stacks api and action (#2512, #2525)
- Avoid Kestrel address override warning (#2535)
- Only create default diagnostic port when running in listen mode (#2523)
- Fixed an issue (#2526) where the `config` command could throw an exception (#2530)

\*🔬 **_indicates an experimental feature_**