
### Was this documentation helpful? [Share feedback](https://www.research.net/r/DGDQWXH?src=documentation%2FlearningPath%2Ftesting)

# Testing

## Running Unit Tests

### Command Line

### Visual Studio

## Types of Tests

### Unit Test

## Functional Tests

[Root assembly](https://github.com/dotnet/dotnet-monitor/tree/main/src/Tests/Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests)

Functional tests are composed of 3 main parts:
- The test itself
- An instance of dotnet-monitor
- An instance of an application that is being monitored

https://github.com/dotnet/dotnet-monitor/blob/0ce33443bf5e19e7f18145c129bc1b2fe68c0213/src/Tests/Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests/LiveMetricsTests.cs#L76-L120

* ScenarioRunner 
* The dotnet-monitor api surface can be accessed through the [ApiClient](../../src/Tests/Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests/HttpApi/ApiClient.cs).
* 


## Test assemblies

### Schema Generation

### OpenAPI generation

### TestCommon

### Http api for tests

### Profiler Test startup hook

### Linking files + UNITTEST