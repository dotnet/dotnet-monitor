Today we are releasing the next official preview of the `dotnet monitor` tool. This release includes:

- Added collection rule templates (#1797)
- Added CPU usage, GC heap size, and threadpool queue length triggers (#1751)
- Added Managed Service Identity support for egress (#1928)
- Added Process.RuntimeId token for collection rule action properties (#1947)
- Fix egress operations to report failed egress to operation service (#1928)
- Prevent process enumeration from pruning processes that are capturing gcdumps (#1932)
- Show warning when using collection rules in `connect` mode (#1830)
- Prevent auth configuration warnings when using `--no-auth` switch (#1838)
