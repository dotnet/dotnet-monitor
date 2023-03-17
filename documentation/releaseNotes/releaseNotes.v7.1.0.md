Today we are releasing the 7.1.0 build of the `dotnet monitor` tool. This release includes:

- Produce RID-specific, TFM-specific, framework dependent archives ([#3507](https://github.com/dotnet/dotnet-monitor/pull/3507))
- Add substitution tokens for other process info ([#3793](https://github.com/dotnet/dotnet-monitor/pull/3793))
- Add Azure Active Directory authentication support. For more details see our [Azure AD documentation](https://github.com/dotnet/dotnet-monitor/blob/main/documentation/authentication.md#azure-active-directory-authentication). ([#3792](https://github.com/dotnet/dotnet-monitor/pull/3792))
- Add support for System.Diagnostics.Metrics ([#3750](https://github.com/dotnet/dotnet-monitor/pull/3750))
- Add Swagger UI to dotnet-monitor as the default endpoint. This will provide documentation of endpoints, and also the ability to use them. ([#3450](https://github.com/dotnet/dotnet-monitor/pull/3450))
- Add Tags For API Requests ([#3162](https://github.com/dotnet/dotnet-monitor/pull/3162))
- Add Metadata Support For Metrics ([#3084](https://github.com/dotnet/dotnet-monitor/pull/3084))
- 🔬 Add support for thread names stack artifacts. ([#3004](https://github.com/dotnet/dotnet-monitor/pull/3004))
- Add support for gracefully stopping a requested trace on demand while also collecting rundown, via [Stop Operation](https://github.com/dotnet/dotnet-monitor/blob/main/documentation/api/operations-stop.md). All artifact requests are now also recorded under the [Operations API](https://github.com/dotnet/dotnet-monitor/blob/main/documentation/api/operations.md), regardless of if they have an egress provider specified. ([#2893](https://github.com/dotnet/dotnet-monitor/pull/2893))
- Add the option to specify a `StoppingEvent` on the `CollectTrace` action. The trace will be stopped once either the duration is reached or the specified event occurs. ([#2884](https://github.com/dotnet/dotnet-monitor/pull/2884))
- Handle exceptions when determining additional information about discovered processes. ([#2813](https://github.com/dotnet/dotnet-monitor/pull/2813))

\*🔬 **_indicates an experimental feature_**