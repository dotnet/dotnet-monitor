# Egress Extensibility

As of version 7.0, `dotnet monitor` adds support for egress extensibility, allowing users to provide their own egress provider in addition to using the existing egress options.

## Creating an Egress Extension

### Overview

If you'd like to contribute your own egress extension, please review our guidelines (**insert link here**) before submitting a pull request at **insert link here**. 

#### Guidelines (Might be hosted on the other GitHub - putting here for now)

By default, egress extensions are considered unverified/experimental (need to settle on official terminology). The basic requirements for an unverified extension include:

- Verify that your extension works correctly and abides by the egress extension contract (**insert link here**)
- Does not include personal credentials to connect to an egress provider
- Does not store user data off-device (wording for clarity?)
- Follows good coding practices

Verified extensions are available via Nuget and must conform to all of the following requirements (in addition to the requirements for unverified extensions):
- Includes unit testing
- Includes a template (**include link here**) for easy set-up
- Has undergone manual validation from a member of the `dotnet monitor` team
- Is considered maintainable by the `dotnet monitor` team*



Main branch -> goes to nuget, approved/verified
Other branches -> Where everything goes initially, still has to go through preliminary review but has less stringent requirements



### 

## Using an Egress Extension

Egress extensions will be available via Nuget at **insert link here** 


### Unverified/Experimental Egress Extensions

Unverified extensions can be found at **insert link here** and are not publicly shipped. To look for a specific egress extension, you can browse the repo's branches.

> **NOTE:** Unverified extensions are community contributions that have not been rigorously tested or otherwise do not meet the requirements for verified extensions (**insert link here**). These extensions do not come with any support guarantees and are not recommended for use in production scenarios.

