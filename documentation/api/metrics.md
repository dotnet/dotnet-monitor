# Metrics - Get

Gets a snapshot of metrics in the Prometheus exposition format of a single process.

The metrics are collected from the following providers by default:
- `System.Runtime`
- `Microsoft.AspNetCore.Hosting`
- `Grpc.AspNetCore.Server`

All of the counters for each of these providers are collected by default.

> **NOTE:** This route collects metrics only from a single process. If there are no processes or more than one process, the endpoint will not return information. In order to facilitate observing a single process, the tool can be configured to listen for connections from a target process; see [Default Process Configuration](<../configuration.md#Default Process Configuration>) and [Diagnostic Port Configuration](<../configuration.md#Diagnostic Port Configuration>) for more details.

## HTTP Route

```http
GET /metrics HTTP/1.1
```

> **NOTE:** Process information (IDs, names, environment, etc) may change between invocations of these APIs. Processes may start or stop between API invocations, causing this information to change.

## Host Address

This route is available on all configured addresses.

## Authentication

Authentication is not enforced for this route.

## Responses

| Name | Type | Description | Content Type |
|---|---|---|---|
| 200 OK | | A list of metrics for a single process in the Prometheus exposition format. | `text/plain` |
| 400 Bad Request | [ValidationProblemDetails](definitions.md#ValidationProblemDetails) | An error occurred due to invalid input. The response body describes the specific problem(s). | `application/problem+json` |
| 401 Unauthorized | | Authentication is required to complete the request. See [Authentication](./../authentication.md) for further information. | |
| 429 Too Many Requests | | There are too many trace requests at this time. Try to request a trace at a later time. | |

## Examples

### Sample Request

```http
GET /metrics HTTP/1.1
Host: localhost:52325
```

### Sample Response

```http
HTTP/1.1 200 OK
Content-Type: text/plain
Transfer-Encoding: chunked

# HELP systemruntime_cpu_usage_ratio CPU Usage
# TYPE systemruntime_cpu_usage_ratio gauge
systemruntime_cpu_usage_ratio 0 1618889176311
systemruntime_cpu_usage_ratio 0 1618889186310
systemruntime_cpu_usage_ratio 0 1618889196305
# HELP systemruntime_working_set_bytes Working Set
# TYPE systemruntime_working_set_bytes gauge
systemruntime_working_set_bytes 22000000 1618889166332
systemruntime_working_set_bytes 22000000 1618889176312
systemruntime_working_set_bytes 22000000 1618889186310
# HELP systemruntime_gc_heap_size_bytes GC Heap Size
# TYPE systemruntime_gc_heap_size_bytes gauge
systemruntime_gc_heap_size_bytes 0 1618889166333
systemruntime_gc_heap_size_bytes 0 1618889176312
systemruntime_gc_heap_size_bytes 0 1618889186310
# HELP systemruntime_gen_0_gc_count Gen 0 GC Count
# TYPE systemruntime_gen_0_gc_count gauge
systemruntime_gen_0_gc_count 0 1618889166335
systemruntime_gen_0_gc_count 0 1618889176313
systemruntime_gen_0_gc_count 0 1618889186311
# HELP systemruntime_gen_1_gc_count Gen 1 GC Count
# TYPE systemruntime_gen_1_gc_count gauge
systemruntime_gen_1_gc_count 0 1618889166336
systemruntime_gen_1_gc_count 0 1618889176314
systemruntime_gen_1_gc_count 0 1618889186311
# HELP systemruntime_gen_2_gc_count Gen 2 GC Count
# TYPE systemruntime_gen_2_gc_count gauge
systemruntime_gen_2_gc_count 0 1618889166336
systemruntime_gen_2_gc_count 0 1618889176314
systemruntime_gen_2_gc_count 0 1618889186312
# HELP systemruntime_exception_count Exception Count
# TYPE systemruntime_exception_count gauge
systemruntime_exception_count 0 1618889166336
systemruntime_exception_count 0 1618889176314
systemruntime_exception_count 0 1618889186312
# HELP systemruntime_threadpool_thread_count ThreadPool Thread Count
# TYPE systemruntime_threadpool_thread_count gauge
systemruntime_threadpool_thread_count 1 1618889166336
systemruntime_threadpool_thread_count 1 1618889176315
systemruntime_threadpool_thread_count 1 1618889186312
# HELP systemruntime_monitor_lock_contention_count Monitor Lock Contention Count
# TYPE systemruntime_monitor_lock_contention_count gauge
systemruntime_monitor_lock_contention_count 0 1618889166336
systemruntime_monitor_lock_contention_count 0 1618889176315
systemruntime_monitor_lock_contention_count 0 1618889186312
# HELP systemruntime_threadpool_queue_length ThreadPool Queue Length
# TYPE systemruntime_threadpool_queue_length gauge
systemruntime_threadpool_queue_length 0 1618889166338
systemruntime_threadpool_queue_length 0 1618889176315
systemruntime_threadpool_queue_length 0 1618889186313
# HELP systemruntime_threadpool_completed_items_count ThreadPool Completed Work Item Count
# TYPE systemruntime_threadpool_completed_items_count gauge
systemruntime_threadpool_completed_items_count 2 1618889166338
systemruntime_threadpool_completed_items_count 2 1618889176315
systemruntime_threadpool_completed_items_count 2 1618889186313
# HELP systemruntime_time_in_gc_ratio % Time in GC since last GC
# TYPE systemruntime_time_in_gc_ratio gauge
systemruntime_time_in_gc_ratio 0 1618889166338
systemruntime_time_in_gc_ratio 0 1618889176315
systemruntime_time_in_gc_ratio 0 1618889186313
# HELP systemruntime_gen_0_size_bytes Gen 0 Size
# TYPE systemruntime_gen_0_size_bytes gauge
systemruntime_gen_0_size_bytes 0 1618889166338
systemruntime_gen_0_size_bytes 0 1618889176315
systemruntime_gen_0_size_bytes 0 1618889186313
# HELP systemruntime_gen_1_size_bytes Gen 1 Size
# TYPE systemruntime_gen_1_size_bytes gauge
systemruntime_gen_1_size_bytes 0 1618889166338
systemruntime_gen_1_size_bytes 0 1618889176316
systemruntime_gen_1_size_bytes 0 1618889186313
# HELP systemruntime_gen_2_size_bytes Gen 2 Size
# TYPE systemruntime_gen_2_size_bytes gauge
systemruntime_gen_2_size_bytes 0 1618889166339
systemruntime_gen_2_size_bytes 0 1618889176316
systemruntime_gen_2_size_bytes 0 1618889186313
# HELP systemruntime_loh_size_bytes LOH Size
# TYPE systemruntime_loh_size_bytes gauge
systemruntime_loh_size_bytes 0 1618889166339
systemruntime_loh_size_bytes 0 1618889176316
systemruntime_loh_size_bytes 0 1618889186313
# HELP systemruntime_alloc_rate_bytes Allocation Rate
# TYPE systemruntime_alloc_rate_bytes gauge
systemruntime_alloc_rate_bytes 40900 1618889166339
systemruntime_alloc_rate_bytes 40900 1618889176316
systemruntime_alloc_rate_bytes 40900 1618889186313
# HELP systemruntime_assembly_count Number of Assemblies Loaded
# TYPE systemruntime_assembly_count gauge
systemruntime_assembly_count 40 1618889166339
systemruntime_assembly_count 40 1618889176316
systemruntime_assembly_count 40 1618889186313
# HELP systemruntime_active_timer_count Number of Active Timers
# TYPE systemruntime_active_timer_count gauge
systemruntime_active_timer_count 0 1618889166339
systemruntime_active_timer_count 0 1618889176316
systemruntime_active_timer_count 0 1618889186313
```

## Supported Runtimes

| Operating System | Runtime Version |
|---|---|
| Windows | .NET Core 3.1, .NET 5+ |
| Linux | .NET Core 3.1, .NET 5+ |
| MacOS | .NET Core 3.1, .NET 5+ |

## Additional Notes

### Custom Metrics

The metrics providers and counter names to return from this route can be specified via configuration. See [Metrics Configuration](<../configuration.md#Metrics Configuration>) for more information.