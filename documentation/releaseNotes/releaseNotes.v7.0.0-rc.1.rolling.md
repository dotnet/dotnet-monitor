Today we are releasing the next official preview of the `dotnet monitor` tool. This release includes:

- With this release, support for experimental features is added. Users can now enable an experimental feature through configuration flags when launching `dotnet monitor` to enable features that are still under development and not yet fully production-ready. (#2506)
- 🔬 Add collect stacks api and action (#2512)
- Avoid Kestrel address override warning (#2535)
- Add `DefaultSharedPath` option for sharing dumps, sockets, and libraries (#2471)

\*🔬 **_indicates an experimental feature_**