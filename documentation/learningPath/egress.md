
### Was this documentation helpful? [Share feedback](https://www.research.net/r/DGDQWXH?src=documentation%2FlearningPath%2Fegress)

# Egress

`dotnet monitor` includes functionality to egress (send) artifacts to permanent storage locations, such as `Azure Blob Storage`. For v8, `dotnet monitor` has converted to an extensible egress model that allows developers to author their own egress providers that aren't included in the default `dotnet monitor` product. This section covers how the egress extensibility model works, and provides information about how to develop an egress extension (using the `AzureBlobStorage` egress provider as an example). 

## How Egress Works

```mermaid
%%{ init: { 'flowchart': { 'curve': 'basis' } } }%%
graph LR
    classDef altColor fill:#CAF,stroke:purple;
    subgraph ide1 [.NET Monitor]
    A[Configuration] --> N{.NET Monitor}
    N --> 1
    N --> 2   
    N --> 3
    end
    subgraph ide2 [Extensions]
    2 ---> R{AzureBlobStorage}
    3 ---> R{AzureBlobStorage}
    R --> 4
    R --> 5
    end
    
    class ide2 altColor
```

1. User initiates collection of artifact with a designated egress provider
1. Find and start extension as separate process
1. Pass configuration/artifact via StdIn
1. Connect to egress provider using configuration and send artifact
1. Provide success/failure information via StdOut to dotnet-monitor


## Distribution and Acquisition Model

## Building An Egress Provider




## Keeping Documentation Up-To-Date

