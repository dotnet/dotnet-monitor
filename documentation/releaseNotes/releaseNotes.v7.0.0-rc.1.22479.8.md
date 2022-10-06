Today we are releasing the official 7.0 Release Candidate of the `dotnet monitor` tool. This release includes:

- With this release, support for experimental features is added. Users can now enable an experimental feature through configuration flags when launching `dotnet monitor`. Experimental features are still under development and not yet fully production-ready. (#2506)
- 🔬 Add collect stacks api and action (#2512, #2525)
- Avoid Kestrel address override warning (#2535)
- Create default diagnostic port under default shared path when running in `listen` mode (#2471, #2523)
- Fixed an issue (#2526) where the `config` command could throw an exception (#2530)
- Fixed an issue where running `config show --show-sources` could incorrectly report `ChainedConfigurationProvider` as the source (#2550)

\*🔬 **_indicates an experimental feature_**