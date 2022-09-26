Today we are releasing the 6.2.0 build of the `dotnet monitor` tool. This release includes:

- Allow for pushing a message to a queue when writing to Azure egress (#163)
- Allow for simplified process filter configuration (#636)
- Allow `config show` command to list configuration sources (#277)
- Added collection rule defaults (#1595)
- Added collection rule templates (#1921)
- Added CPU usage, GC heap size, and threadpool queue length triggers (#1911)
- Added Managed Service Identity support for egress (#1884)
- Added Process.RuntimeId token for collection rule action properties (#1870)
- Fix egress operations to report failed egress to operation service (#1884)
- Prevent process enumeration from pruning processes that are capturing gcdumps (#1927)
- Show warning when using collection rules in `connect` mode (#1844)
- Prevent auth configuration warnings when using `--no-auth` switch (#1845)
