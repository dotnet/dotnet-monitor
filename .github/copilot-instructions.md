# dotnet-monitor Repository Instructions

Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

## Working Effectively

### Repository Overview
`dotnet-monitor` is a .NET diagnostic tool for capturing logs, traces, process dumps, and metrics from .NET applications in both interactive and automated scenarios. The repository contains:
- **27 C# projects** across tools, libraries, tests, and extensions  
- **Main tool**: `src/Tools/dotnet-monitor` (ASP.NET Core web application)
- **Test projects**: Unit tests, functional tests, profiler tests, configuration schema tests
- **Extensions**: Azure Blob Storage, S3 Storage egress providers
- **Profilers**: Native code for in-process monitoring capabilities

### CRITICAL: Build System Requirements

**⚠️ NETWORK ACCESS REQUIRED**: The build system requires access to Microsoft Azure DevOps package feeds:
- `pkgs.dev.azure.com/dnceng/*` domains  
- `1oavsblobprodcus350.vsblob.vsassets.io` (Azure blob storage)

**Build fails with firewall restrictions** due to inability to download `Microsoft.DotNet.Arcade.Sdk.9.0.0-beta.25407.2`.

### Prerequisites  
- **.NET 9.0.109 SDK** (automatically installed by build scripts)
- **Docker** (for container-based development and testing)
- **Network access** to Microsoft Azure DevOps feeds (essential)
- **Linux/Windows/macOS** supported
- **4+ CPU cores recommended** for development containers

### Build Commands (When Network Access Available)

**NEVER CANCEL builds or tests** - timing expectations:
- **Restore**: 5-10 minutes (depends on network)
- **Build**: 15-45 minutes (NEVER CANCEL - set timeout 60+ minutes) 
- **Tests**: 15-30 minutes (NEVER CANCEL - set timeout 45+ minutes)

```bash
# Bootstrap and build (Linux/macOS)
./build.sh
# Expected time: 15-45 minutes - NEVER CANCEL

# Build on Windows  
.\Build.cmd
# Expected time: 15-45 minutes - NEVER CANCEL

# Run tests (Linux/macOS)
./test.sh
# Expected time: 15-30 minutes - NEVER CANCEL  

# Run tests on Windows
.\Test.cmd
# Expected time: 15-30 minutes - NEVER CANCEL

# Test specific groups
./build.sh -test -testgroup PR
# Runs same tests as PR build
```

### Alternative Development Approaches

**When build fails due to network restrictions:**

1. **Use prebuilt containers** for functionality testing (RECOMMENDED):
```bash
# Run latest nightly build
docker run --rm mcr.microsoft.com/dotnet/nightly/monitor:9.0 --help

# Test collect command
docker run --rm mcr.microsoft.com/dotnet/nightly/monitor:9.0 collect --help

# Test with sample app (Docker Compose) - VALIDATED WORKING
cd samples/Docker
docker compose up
# Provides dotnet-monitor on :52323 (API) and :52325 (metrics) 
# Sample ASP.NET app on :8080
# Clean up: docker compose down
```

2. **Global tool installation** (MAY FAIL due to network restrictions):
```bash
# Latest stable - may fail with "Settings file 'DotnetToolSettings.xml' was not found"
dotnet tool install -g dotnet-monitor

# Latest nightly - requires access to dnceng.pkgs.visualstudio.com 
dotnet tool install -g dotnet-monitor --add-source https://dnceng.pkgs.visualstudio.com/public/_packaging/dotnet-tools/nuget/v3/index.json --prerelease
```

3. **Use DevContainer** for isolated development:
- Requires Docker and VS Code
- Pre-configured with all dependencies  
- Includes .NET 9, cmake, clang, Node.js, Azure CLI
- Located in `.devcontainer/devcontainer.json`
- May still have same network connectivity issues

### Project Structure Navigation

**Key directories:**
- `src/Tools/dotnet-monitor/` - Main CLI tool and web API
- `src/Microsoft.Diagnostics.Monitoring.WebApi/` - Core API implementation  
- `src/Microsoft.Diagnostics.Monitoring.Options/` - Configuration models
- `src/Tests/` - All test projects (20 test assemblies)
- `src/Profilers/` - Native profiler libraries
- `documentation/` - Comprehensive docs including APIs, configuration
- `samples/` - Docker, Kubernetes, AKS, Grafana examples
- `eng/` - Build infrastructure (Arcade SDK)

**Important files:**
- `global.json` - .NET SDK version requirements
- `NuGet.config` - Package feed configuration
- `dotnet-monitor.sln` - Main solution file
- `documentation/api/README.md` - API documentation
- `documentation/building.md` - Build instructions

### Test Execution

**Test Categories:**
- **Unit tests**: Direct component testing with mocked dependencies
- **Functional tests**: End-to-end scenarios with real dotnet-monitor and test apps
- **Profiler tests**: Native code integration tests  
- **Schema tests**: Configuration validation

**Test timing expectations:**
- Unit tests: 2-5 minutes per assembly
- Functional tests: 10-20 minutes (most time-consuming)
- Full test suite: 15-30 minutes total

**Key test projects:**
- `Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests` - Main E2E tests
- `Microsoft.Diagnostics.Monitoring.Tool.UnitTests` - Tool unit tests
- `Microsoft.Diagnostics.Monitoring.WebApi.UnitTests` - API unit tests

### Validation Scenarios

**Always test these scenarios after making changes:**

1. **CLI functionality** (using containers when build unavailable):
```bash
# Using container
docker run --rm mcr.microsoft.com/dotnet/nightly/monitor:9.0 --help
docker run --rm mcr.microsoft.com/dotnet/nightly/monitor:9.0 collect --help
docker run --rm mcr.microsoft.com/dotnet/nightly/monitor:9.0 config show

# Using global tool (if installed)
dotnet-monitor --help
dotnet-monitor collect --help 
dotnet-monitor config show
```

2. **Container + Sample App integration** (VALIDATED WORKING):
```bash
cd samples/Docker
docker compose up -d

# Test endpoints
curl http://localhost:52323/processes        # Should show process info
curl http://localhost:52325/metrics | head   # Should show Prometheus metrics
curl http://localhost:8080/                  # Should show sample ASP.NET app
 
# Clean up
docker compose down
```

3. **API endpoints** (when running):
- Health: `GET http://localhost:52323/healthz`
- Process list: `GET http://localhost:52323/processes`  
- Metrics: `GET http://localhost:52325/metrics`

4. **Configuration validation**:
- Test schema generation: `src/Tests/Microsoft.Diagnostics.Monitoring.ConfigurationSchema.UnitTests`
- Validate settings files in `src/Tools/dotnet-monitor/appsettings.json`

### Common Development Tasks

**Adding new API endpoints:**
1. Define in `src/Microsoft.Diagnostics.Monitoring.WebApi/Controllers/`
2. Add models in `src/Microsoft.Diagnostics.Monitoring.Options/`
3. Update OpenAPI generation in `src/Tests/Microsoft.Diagnostics.Monitoring.OpenApiGen/`
4. Add functional tests in `src/Tests/Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests/`

**Modifying configuration:**
1. Update options classes in `src/Microsoft.Diagnostics.Monitoring.Options/`
2. Regenerate schema: Run `Microsoft.Diagnostics.Monitoring.ConfigurationSchema.UnitTests`
3. Update documentation in `documentation/configuration/`

**Working with collection rules:**
- Core logic: `src/Tools/dotnet-monitor/CollectionRules/`
- Tests: `src/Tests/CollectionRuleActions.UnitTests/`
- Documentation: `documentation/collectionrules/`

### Troubleshooting Build Issues

**Common problems:**
1. **Arcade SDK download fails**: Network/firewall blocking Azure DevOps feeds
2. **Restore hangs**: Clear NuGet cache with `dotnet nuget locals all --clear`
3. **Tests timeout**: Increase timeout values, tests can take 15+ minutes
4. **Native build fails**: Ensure cmake and clang are installed

**Workarounds when build fails:**
- Use prebuilt containers for testing functionality
- Install global tool for API testing  
- Use DevContainer for isolated environment
- Focus on documentation and configuration changes

### CI/CD Integration

**Before committing always run:**
- Build validation (if network allows)
- Relevant test suites for changed components  
- `dotnet format` for code formatting (if .NET SDK available)
- Check documentation updates match code changes
- Validate Docker Compose samples work with changes
- Run spell check (cspell) if Node.js available

**Code quality tools:**
- **EditorConfig**: `.editorconfig` defines consistent formatting
- **Spell checking**: `cspell.json` configuration for markdown/code
- **Code formatting**: `dotnet format` (requires .NET SDK)

**Pipeline details:**
- Main pipeline: `eng/pipelines/dotnet-monitor-public.yml`
- Test groups: Default, All, None, PR
- Supports multiple platforms: Windows, Linux, macOS

### Performance Considerations

**Resource requirements:**
- **Memory**: 512MB minimum for dotnet-monitor, 50MB for monitoring overhead
- **CPU**: 0.25 CPU cores typical, can burst higher during collection
- **Storage**: Minimal for logs/config, significant for dumps/traces
- **Network**: Low bandwidth except during artifact egress

### Docker and Container Usage

**Key container images:**
- `mcr.microsoft.com/dotnet/monitor:8` - Latest stable
- `mcr.microsoft.com/dotnet/nightly/monitor:9.0` - Nightly builds
- Contains tool + profilers + extension libraries

**Container configuration:**
- Default ports: 52323 (HTTPS API), 52325 (HTTP metrics)
- Volume mounts: `/diag` for shared diagnostic ports
- Environment variables: `DOTNETMONITOR_*` for configuration

**Sample deployment** (VALIDATED WORKING):
```bash
cd samples/Docker
docker compose up -d
# Demonstrates app + monitor containers with shared volumes  
# Validates: API on :52323, metrics on :52325, sample app on :8080
# Clean up: docker compose down
```

### Documentation Updates

**When making changes, also update:**
- API docs in `documentation/api/` if adding/changing endpoints
- Configuration docs in `documentation/configuration/` if changing options  
- Schema file `documentation/schema.json` via test generation
- OpenAPI spec `documentation/openapi.json` via test generation
- Release notes in `documentation/releaseNotes/` for significant changes

This repository has extensive documentation - always check existing docs before creating new ones.