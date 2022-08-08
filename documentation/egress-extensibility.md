# Egress Extensibility

As of version 7.0, `dotnet monitor` adds support for egress extensibility, allowing users to provide their own egress provider in addition to using the existing egress options.

## Creating an Egress Extension

### Overview

If you'd like to contribute your own egress extension, please review our guidelines (**insert link here**) before submitting a pull request at **insert link here**. 

By default, extensions are considered `unverified` and can be found in feature branches in the GitHub repo. 

#### `Unverified` vs. `Verified`

#### Guidelines (Might be hosted on the other GitHub - putting here for now)

The basic requirements for an `unverified` extension include:

- Verify that your extension works correctly and abides by the egress extension contract (**insert link here**)
- Does not include personal credentials to connect to an egress provider
- Does not store user data off-device (wording for clarity?)
- Does not duplicate the functionality of another egress extension (improvements and changes should be directly made to the existing extension)
- Follows good coding practices
- Includes basic documentation

`Verified` extensions are available via Nuget and must conform to all of the following requirements (in addition to the requirements for `unverified` extensions):
- Includes unit testing
- Includes a template (**include link here**) for easy set-up
- Has undergone manual validation from a member of the `dotnet monitor` team
- Is considered maintainable by the `dotnet monitor` team*




### 

## Using an Egress Extension

Egress extensions will be available via Nuget at **insert link here** 


### `Unverified` Egress Extensions

Unverified extensions can be found at **insert link here** and are not publicly shipped. To look for a specific egress extension, you can browse the repo's branches.

> **NOTE:** Unverified extensions are community contributions that have not been rigorously tested or otherwise do not meet the requirements for verified extensions (**insert link here**). These extensions do not come with any support guarantees and are not recommended for use in production scenarios.


## `Unverified` vs. `Verified`

`Unverified` extensions are only available as feature branches in the GitHub repo, and are subjected to less stringent requirements than `Verified` extensions (**Insert link to guidelines here**). `Unverified` extensions provide the community with a way to share their contributions without requiring the extra time and effort needed to make the code production-ready. To verify an `unverified` extension, any member of the community can make the required changes to an existing `unverified` extension and open a Pull Request into the main branch. If verified, the extension will be included as part of the `dotnet monitor extensions` nuget package (**Insert link here and include correct name of nuget package**).
