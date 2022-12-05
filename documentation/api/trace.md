
### Was this documentation helpful? [Share feedback](https://www.research.net/r/DGDQWXH?src=documentation%2Fapi%2Ftrace)

# Traces

The Traces API enables collecting `.nettrace` formatted traces without using a profiler.

> **Note**: Process information (IDs, names, environment, etc) may change between invocations of these APIs. Processes may start or stop between API invocations, causing this information to change.

| Operation | Description |
|---|---|
| [Get Trace](trace-get.md) | Captures a diagnostic trace of a process based on a predefined set of trace profiles. |
| [Get Custom Trace](trace-custom.md) | Captures a diagnostic trace of a process using the given set of event providers specified in the request body. |
