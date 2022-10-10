Today we are releasing the 6.1.2 build of the `dotnet monitor` tool. This release includes:

- Ensure unexpected egress failures complete operations (#1935)
- Prevent process enumeration from pruning processes that are capturing gcdumps (#1933)
- Prevent auth configuration warnings when using `--no-auth` switch (#1851)
- Show warning when using collection rules in `connect` mode (#1852)
