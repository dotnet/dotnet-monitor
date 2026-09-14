# Collection Rules

`dotnet monitor` can be [configured](../configuration/collection-rule-configuration.md#collection-rule-configuration) to automatically collect diagnostic artifacts based on conditions within the discovered processes.

> [!NOTE]
> Collection rules are only enabled when running dotnet-monitor in `Listen` mode. See [Connection Mode](../configuration/collection-rule-configuration.md#connection-mode) configuration for details.

A collection rule is composed of four key aspects:
- [Filters](#filters): Describes for which processes the rule is applied. Can filter on aspects such as process name, ID, and command line.
- [Trigger](#triggers): A condition to monitor in the target process.
- [Actions](#actions): A list of actions to execute when the trigger condition is satisfied.
- [Limits](#limits): Limits applied to the rule or action execution.

## Behavior

When a process is newly discovered by `dotnet monitor`, the tool will attempt to apply all of the configured collection rules. If a process matches the [filters](#filters) on a rule or the rule does not have any filters, then the rule is applied to the process.

An applied rule will start its [trigger](#triggers) on the process, monitoring for the condition that the trigger describes. If the trigger is the `Startup` trigger, the trigger is immediately satisfied.

Once a trigger is satisfied, the [action](#actions) list is executed. Each action is started (see [Action List Execution](#action-list-execution) for more details) in the order as specified by the list of actions. When the execution of the action list is completed, the rule will restart the [trigger](#triggers) to begin monitoring for the condition that the trigger describes.

[Limits](#limits) can be applied to inform the rule of how long the rule may run, how many times the action list may be executed, etc.

## Filters

A rule can describe for which processes that the rule is applied. If a discovered process does not match the filters, then the rule is not applied to the process. If filters are not configured, the rule is applied to the process.

> [!NOTE]
> `dotnet monitor` is capable of observing multiple processes simultaneously. The filter mechanism for collection rules allows the user to specify which subset of the observed processes that each individual rule should be applied.

The filter criteria are the same as those used for the [default process](../configuration/collection-rule-configuration.md#default-process-configuration) configuration.

See [Filters](../configuration/collection-rule-configuration.md#filters) configuration for details and an example of how to specify the filters.

## Triggers

A trigger will monitor for a specific condition in the target application and raise a notification when that condition has been observed.

The following are the currently available triggers:

| Name | Type | Description |
|---|---|---|
| Startup | Startup | Satisfied immediately when the rule is applied to a process. |
| [AspNetRequestCount](../configuration/collection-rule-configuration.md#aspnetrequestcount-trigger) | Event Pipe | Satisfied when the number of HTTP requests is above the threshold count. |
| [AspNetRequestDuration](../configuration/collection-rule-configuration.md#aspnetrequestduration-trigger) | Event Pipe | Satisfied when the number of HTTP requests have response times longer than the threshold duration. |
| [AspNetResponseStatus](../configuration/collection-rule-configuration.md#aspnetresponsestatus-trigger) | Event Pipe | Satisfied when the number of HTTP responses that have status codes matching the pattern list is above the specified threshold. |
| [EventCounter](../configuration/collection-rule-configuration.md#eventcounter-trigger) | Event Pipe | Satisfied when the value of a counter falls above, below, or between the described threshold. |
| [EventMeter](../configuration/collection-rule-configuration.md#eventmeter-trigger-80) | Event Pipe | Satisfied when the value of an instrument falls above, below, or between the described threshold. |

## Actions

Actions allow executing an operation or an external executable in response to a trigger condition being satisfied. Each type of action may have outputs that are consumable by other action settings using an action output dependency (see [Action Output Dependencies](#action-output-dependencies) below).

The following are the currently available actions:

| Name | Description |
|---|---|
| [CollectDump](../configuration/collection-rule-configuration.md#collectdump-action) | Collects a memory dump of the target process. |
| [CollectExceptions](../configuration/collection-rule-configuration.md#collectexceptions-action) | Collects exceptions from the target process. |
| [CollectGCDump](../configuration/collection-rule-configuration.md#collectgcdump-action) | Collects a gcdump of the target process. |
| [CollectLiveMetrics](../configuration/collection-rule-configuration.md#collectlivemetrics-action) | Collects live metrics from the target process. |
| [CollectLogs](../configuration/collection-rule-configuration.md#collectlogs-action) | Collects logs from the target process. |
| [CollectStacks](../configuration/collection-rule-configuration.md#collectstacks-action) | Collects call stacks from the target process. |
| [CollectTrace](../configuration/collection-rule-configuration.md#collecttrace-action) | Collects an event trace of the target process. |
| [Execute](../configuration/collection-rule-configuration.md#execute-action) | Executes an external executable with command line parameters. |
| [LoadProfiler](../configuration/collection-rule-configuration.md#loadprofiler-action) | Loads an ICorProfilerCallback implementation into the target process. |
| [SetEnvironmentVariable](../configuration/collection-rule-configuration.md#setenvironmentvariable-action) | Sets an environment variable value in the target process. |
| [GetEnvironmentVariable](../configuration/collection-rule-configuration.md#getenvironmentvariable-action) | Gets an environment variable value from the target process. |

## Limits

See [Limits](../configuration/collection-rule-configuration.md#limits) for details on the configurable limits.

## Advanced Behavior

### Action Output Dependencies

Actions may reference the outputs of other actions that have started before them in the same action list execution. These dependencies are described using a simple syntax within the settings of the action. The syntax is:

`$(Actions.<ActionName>.<OutputName>)`

where `<ActionName>` is the name of the action from which to get an output value and `<OutputName>` is the name of an output from that action.

For example, if action `A` has an output named `EgressPath`, and action `B` has a settings property named `Arguments`, then action `B` can reference the `EgressPath` from within the `Arguments` property setting:

<details>
  <summary>JSON</summary>

  ```json
  {
      "Actions": [{
          "Name": "A",
          "Type": "CollectTrace",
          "Settings": {
              "Profile": "Cpu",
              "Egress": "AzureBlob"
          }
      },{
          "Name": "B",
          "Type": "Execute",
          "Settings": {
              "Path": "path-to-dotnet",
              "Arguments": "MyApp.dll $(Actions.A.EgressPath)"
          }
      }]
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  CollectionRules__RuleName__Actions__0__Name: "A"
  CollectionRules__RuleName__Actions__0__Type: "CollectTrace"
  CollectionRules__RuleName__Actions__0__Settings__Profile: "Cpu"
  CollectionRules__RuleName__Actions__0__Settings__Egress: "AzureBlob"
  CollectionRules__RuleName__Actions__1__Name: "B"
  CollectionRules__RuleName__Actions__1__Type: "Execute"
  CollectionRules__RuleName__Actions__1__Settings__Path: "path-to-dotnet"
  CollectionRules__RuleName__Actions__1__Settings__Arguments: "MyApp.dll $(Actions.A.EgressPath)"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__0__Name
    value: "A"
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__0__Type
    value: "CollectTrace"
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__0__Settings__Profile
    value: "Cpu"
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__0__Settings__Egress
    value: "AzureBlob"
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__1__Name
    value: "B"
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__1__Type
    value: "Execute"
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__1__Settings__Path
    value: "path-to-dotnet"
  - name: DotnetMonitor_CollectionRules__RuleName__Actions__1__Settings__Arguments
    value: "MyApp.dll $(Actions.A.EgressPath)"
  ```
</details>

At this time, only the `Arguments` property of the `Execute` action may use an action output dependency.

### Additional token substitution

In addition to dependencies, the following list of token substitutions are also available for action settings:

| Name | Description |
|---|---|
| `$(Process.RuntimeId)` | The unique identifier of the target process. Note for 3.1 applications, this will be the empty Guid. |
| `$(Process.ProcessId)` | Process id of the target process. |
| `$(Process.Name)` | Name of the target process. |
| `$(Process.CommandLine)` | Command line of the target process. |

### Action List Execution

When the action list of a rule is executed, the actions are started in the order in which they were specified within the list. However, each action may be completed asynchronously (this is the default behavior). For example, if an action list has actions `A`, `B`, `C`, then the execution of the list is:

1. Start `A`
1. Start `B`
1. Start `C`
1. Wait for all actions to complete.

The execution of this list **does not** wait for action `A` to complete before starting action `B`; similarly, action `B` completion is not awaited before starting action `C`. The execution of the list will wait for all actions to complete before the execution of the list is considered completed.

The above behavior can be changed with the `WaitForCompletion` property on individual actions or using action output dependencies.

If `WaitForCompletion` is set to `true` on an action, the execution of the list will wait for that action to complete **before** starting the next action. Using the same `A`, `B`, `C` example, if action `B` has `WaitForCompletion` set to `true`, then the execution of the list is:

1. Start `A`
1. Start `B`
1. Wait for `B` to complete
1. Start `C`
1. Wait for all remaining actions (namely, `A` and `C`) to complete.

If an action has an output dependency on another action, the execution of the list will wait for the dependency to complete before starting the dependent action. Using the same `A`, `B`, `C` example, if action `C` has has an output dependency on action `A`, then the execution of the list is:

1. Start `A`
1. Start `B`
1. Wait for `A` to complete
1. Start `C`
1. Wait for all remaining actions (namely, `B` and `C`) to complete.

### Resume Runtime

Typically, when a target process is connecting to a `dotnet monitor` instance running in `Listen` mode, the target process runtime is suspended until `dotnet monitor` instructs it to resume. This allows starting diagnostic operations before the runtime starts, so that events that occur early in the runtime execution may be captured.

The rule system plays a part in this process by starting the triggers of all of the applicable rules on a process **before** its runtime is resumed. For the `Startup` trigger, the rule will not yield back to the runtime for resumption until all actions are started; the caveat is that when starting the actions, if either an explicit wait is specified via the `WaitForCompletion` setting or an action dependency forces an implicit wait, then the runtime is resumed at that point. For non-`Startup` trigger rules, the rule will yield back to the runtime for resumption as soon as the trigger has started.

## Examples

For examples of how to configure collection rules, see [Collection Rule Examples](collectionruleexamples.md).
