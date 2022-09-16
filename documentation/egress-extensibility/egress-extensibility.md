# Egress Extensibility

As of version 7.0, `dotnet monitor` adds support for egress extensibility, allowing users to provide their own egress providers in addition to using the existing egress options.

## Creating an Egress Extension

### Overview

The new egress extensibility model enables developers to create their own extensions without having to go through the process of submitting a pull request on the `dotnet-monitor` GitHub repository. Any developer can make an extension that cooperates with `dotnet-monitor` by following our contract **insert link here** that describes how data, credentials, and configuration are passed between `dotnet-monitor` and the extension.

By default, developers will retain full ownership and control over their extensions; however, we encourage contributors to share their extensions with the community to improve the `dotnet-monitor` experience for other users. **The `dotnet-monitor` repo will not officially endorse any third-party extensions or guarantee their functionality/support**; however, we may link third-party extensions from our repo for users to explore at their own risk.

The `dotnet-monitor-egress-extensions`(**verify name**) is the new home of the `Azure Blob Storage` egress provider and select third-party egress providers that meet our guidelines (**insert link here**). The `dotnet-monitor` team will actively support and distribute these extensions as detailed here (**insert link here**). 

If you'd like to contribute your own egress extension, please review our guidelines (**insert link here**) before submitting a pull request at **insert link here**. 

#### Guidelines For Submitting An Extension to `dotnet-monitor-egress-extensions`(**verify name**)

The basic requirements for an extension include:

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

### 

## Using an Egress Extension

Officially supported egress extensions will be available via Nuget at **insert link here**. There are two ways to incorporate these extensions with your `dotnet-monitor` instance:

- Users will be able to install extensions from Nuget via `dotnet tool install`, which will automatically place the extension in the `.dotnet\tools` directory, where `dotnet-monitor` knows to look for extensions. 
- Users will be able to manually download Nuget packages and place the contents of the package into one of the locations where `dotnet-monitor` looks for extensions **insert link here**. 

Third party egress extensions that are not included in the `dotnet-monitor-egress-extensions`(**verify name**) repo will have their own distribution mechanisms and guidelines. The `dotnet-monitor` team has guidelines **insert link here** that encourage best practices to help users have a consistent experience when interacting with extensions; **however, these extensions are independently controlled and not reviewed by the `dotnet-monitor` team.**
