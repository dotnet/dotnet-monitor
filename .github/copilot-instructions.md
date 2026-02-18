# dotnet-monitor Repository - Copilot Instructions

## Project Overview

`dotnet-monitor` is a diagnostic tool for capturing diagnostic artifacts from .NET applications in an operator-driven or automated manner. It provides a unified HTTP API for collecting diagnostics regardless of where the application runs (local, Docker, Kubernetes).

**Key Features:**
- HTTP API for on-demand collection of diagnostic artifacts (dumps, traces, logs, metrics)
- Collection Rules for automated, rule-based artifact collection based on triggers
- Egress providers for storing artifacts in various destinations
- Cross-platform support (Windows, Linux, macOS)

## Technology Stack

- **Primary Language:** C# 
- **.NET SDK:** 10.0
- **Target Frameworks:** .NET 8.0, 9.0, 10.0
- **ASP.NET Core:** Multi-version support (8.0, 9.0, 10.0)
- **Build System:** MSBuild with .NET Arcade SDK
- **Native Code:** C++ (for profilers and native components)
- **Testing:** xUnit
- **Documentation:** Markdown
- **Package Management:** NuGet with Central Package Management

## Repository Structure

- **`src/Tools/dotnet-monitor/`** - Main dotnet-monitor tool
- **`src/Microsoft.Diagnostics.Monitoring.WebApi/`** - Core Web API implementation
- **`src/Microsoft.Diagnostics.Monitoring.Options/`** - Configuration options
- **`src/Microsoft.Diagnostics.Monitoring.StartupHook/`** - Startup hook for in-process features
- **`src/Extensions/`** - Extension implementations (Azure Blob, S3 storage, etc.)
- **`src/Profilers/`** - Native profiler implementations
- **`src/Tests/`** - All test projects
- **`documentation/`** - User-facing documentation
- **`eng/`** - Build and engineering infrastructure

## Build and Test Commands

### Build
```bash
# Windows
.\Build.cmd

# Linux/macOS
./build.sh
```

### Test
```bash
# Windows
.\Test.cmd

# Linux/macOS  
./test.sh
```

### Restore Dependencies
```bash
# Windows
.\Restore.cmd

# Linux/macOS
./restore.sh
```

## Coding Guidelines

### C# Code Style

1. **File Headers:** All C# files must include the .NET Foundation license header:
   ```csharp
   // Licensed to the .NET Foundation under one or more agreements.
   // The .NET Foundation licenses this file to you under the MIT license.
   ```

2. **Language Version:** Use `Latest` C# language features

3. **Code Style Preferences:**
   - Do NOT use `var` - use explicit types
   - Prefer explicit types for locals, parameters, and member access
   - Use `this.` qualification: NO (avoid using `this.` unless necessary)
   - Expression-bodied members: Use for properties, accessors, indexers, lambdas
   - Expression-bodied members: DO NOT use for methods, constructors, operators
   - Pattern matching: Prefer pattern matching over `as` with null checks
   - Null-checking: Use null-conditional operators and null-coalescing
   - Using directives: Place outside namespace
   - Namespace style: Use block-scoped namespaces (not file-scoped)
   - Braces: Always use braces even for single-line blocks

4. **Modifier Order:**
   ```
   public, private, protected, internal, static, extern, new, virtual, 
   abstract, sealed, override, readonly, unsafe, volatile, async
   ```

5. **Documentation:** Generate XML documentation files for all projects

6. **Warnings:** Treat all warnings as errors (`TreatWarningsAsErrors=true`)

7. **Code Style Enforcement:** Code style rules are enforced during build (`EnforceCodeStyleInBuild=true`)

### Formatting

- **Indentation:** 4 spaces (2 spaces for XML/YAML/JSON config files)
- **Line Endings:** Insert final newline
- **Trailing Whitespace:** Trim trailing whitespace
- **C++ Code:** Use Allman brace style

### Testing

- All test projects are in `src/Tests/`
- Use xUnit for unit tests
- Test project naming: `<Component>.UnitTests` or `<Component>.IntegrationTests`
- Always add tests when adding new features
- Start with a failing test when fixing bugs

## Security Considerations

1. **Security Reporting:** Report security issues privately to secure@microsoft.com (MSRC)
2. **Authentication:** dotnet-monitor supports API Key authentication (recommended) and Windows authentication
3. **Sensitive Data:** Never commit secrets or API keys to source code
4. **Security Documentation:** See `documentation/security-considerations.md` for security topics

## Pull Request Process

1. Create an issue for non-trivial changes
2. Fork the repository and create a feature branch from `main`
3. Make focused, minimal changes
4. Add tests for new features or bug fixes
5. Ensure builds are clean and all tests pass
6. Sign the .NET Foundation CLA (automated on first PR)
7. Wait for review from Microsoft team members
8. Address feedback and iterate

## Key Configuration Files

- **`Directory.Build.props`** - Common MSBuild properties for all projects
- **`Directory.Packages.props`** - Central package version management
- **`.editorconfig`** - Code style and formatting rules
- **`global.json`** - .NET SDK version pinning
- **`NuGet.config`** - NuGet feed configuration

## Common Patterns

1. **Options Pattern:** Use strongly-typed options classes in `Microsoft.Diagnostics.Monitoring.Options`
2. **Collection Rules:** Implement triggers and actions following existing patterns
3. **Egress Providers:** Extend `IEgressProvider` for new storage backends
4. **API Endpoints:** Follow RESTful conventions in `Microsoft.Diagnostics.Monitoring.WebApi`
5. **Configuration:** Use IConfiguration with JSON/environment variable support

## Documentation

- User documentation in `documentation/` directory
- API documentation at `documentation/api/`
- Configuration schema: `documentation/schema.json`
- Keep documentation synchronized with code changes
- Follow existing markdown structure and formatting

## Useful Links

- [Contributing Guidelines](CONTRIBUTING.md)
- [Building Instructions](documentation/building.md)
- [Security Policy](SECURITY.md)
- [API Documentation](documentation/api/README.md)
- [Configuration Guide](documentation/configuration/README.md)
