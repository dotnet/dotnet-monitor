# Contributing Guidelines

The following guidelines describe how data, credentials, and configuration are passed between `dotnet-monitor` and extensions, as well as best practices to help provide all users with a consistent experience regardless of their preferred egress provider.

### Configuration

Configuration for extensions can be written alongside `dotnet-monitor` configuration, allowing users to keep all of their documentation in one place. However, the `dotnet-monitor` schema will not include information about third-party extensions, which means that users writing `json` configuration will not be able to benefit from suggestions or autocomplete. To address this issue, we encourage all extensions to include a template (**insert link here**) and a schema that users can rely on when writing configuration.

### Logging

Console output from the extension is designed to be forwarded to `dotnet monitor`, which will log messages from the extension alongside its own logs. `dotnet monitor` will also disambiguate between standard output and error output.

### Documentation

In order to help users interface with your extension, we recommend including documentation that specifies its expected behavior and the set-up process. For an example of how to describe your extension's configuration, reference our Azure Blob Storage Extension's [documentation](**insert link here**).

## Guidelines For Submitting An Extension to `dotnet-monitor-egress-extensions`(**verify name**)

The `dotnet-monitor-egress-extensions`(**verify name**) repo is the new home of the `Azure Blob Storage` egress provider and select third-party egress providers. The `dotnet-monitor` team will actively support and distribute these extensions as detailed here (**insert link here**). In order for an extension to be added as a supported egress provider, it should meet the following requirements:

- Verify that your extension works correctly and abides by the egress extension contract (**insert link here**)
- Does not include personal credentials to connect to an egress provider
- Does not store user data off-device (wording for clarity?)
- Does not duplicate the functionality of another egress extension (improvements and changes should be directly made to the existing extension)
- Follows good coding practices
- Includes thorough documentation
- Includes unit testing
- Includes a template (**include link here**) for easy set-up
- Has undergone manual validation from a member of the `dotnet monitor` team
- Is considered maintainable by the `dotnet monitor` team*

> **NOTE:** The `dotnet-monitor` team reserves the right to control which extensions are included in the `dotnet-monitor-egress-extensions`(**verify name**) repo - **completion of the aforementioned requirements is not a guarantee that an extension's submission will be accepted**. As a result, we recommend that you start a discussion on our GitHub page regarding your proposed extension before opening a pull request.
