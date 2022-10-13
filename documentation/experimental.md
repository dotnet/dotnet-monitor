# Experimental Features

Some features are offered as experimental, meaning that they are not supported however they can be enabled to evaluate their current state. These features may or may not ship with full support in future releases and can be redesigned or removed in any future release.

The following are the current set of experimental features:

| Name | Description | First Available Version | How to Enable |
|---|---|---|---|
| Call Stacks | Collect call stacks from target processes as a diagnostic artifact using either the `/stacks` route or the `CollectStacks` collection rule action. | 7.0 RC 1 | Set `DotnetMonitor_Experimental_Feature_CallStacks` to `true` as an environment variable on the `dotnet monitor` process or container. |
