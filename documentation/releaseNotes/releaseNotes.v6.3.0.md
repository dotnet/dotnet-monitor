Today we are releasing the 6.3.0 build of the `dotnet monitor` tool. This release includes:

- Add metadata for Azure egress (#2371)
- Support restrictive SAS tokens for Azure egress (#2325, #2394)
- Avoid Kestrel address override warning (#2576)
- Check read permissions for JSON configuration files (#2272)
- Improve `operations` Api to show RuntimeInstanceId and support filtering (#2265, #2360)
- Fixed an issue (#2526) where the `config` command could throw an exception (#2551)
- Fixed an issue where running `config show --show-sources` could incorrectly report `ChainedConfigurationProvider` as the source (#2553)