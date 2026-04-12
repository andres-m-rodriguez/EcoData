# Aspire.Hosting.Zig

A .NET Aspire hosting extension for building and running Zig applications.

## Usage

```csharp
var zigApp = builder
    .AddZigApp("my-zig-app", "../path/to/zig/project")
    .WithOptimization(ZigOptimizeMode.Debug)
    .WithZigHttpEndpoint(8080)
    .WithHttpHealthCheck("/health");
```

## Features

- **Automatic Build**: Zig projects are automatically built before the application starts using the `BeforeStartEvent` subscription
- **Optimization Modes**: Support for Debug, ReleaseSafe, ReleaseFast, and ReleaseSmall
- **HTTP Endpoints**: Configure HTTP endpoints for service discovery and health checks
- **Custom Build/Run Steps**: Override the default `install` and `run` steps
- **Dashboard Commands**: Optional watch and rebuild commands for the Aspire dashboard

## Extension Methods

| Method | Description |
|--------|-------------|
| `AddZigApp()` | Adds a Zig application to the AppHost |
| `WithOptimization()` | Sets the optimization mode |
| `WithZigHttpEndpoint()` | Configures an HTTP endpoint |
| `WithBuildStep()` | Overrides the build step (default: "install") |
| `WithRunStep()` | Overrides the run step (default: "run") |
| `WithZigPath()` | Sets a custom path to the Zig executable |
| `WithZigArgs()` | Passes additional arguments to the application |
| `WithTests()` | Configures the app to run tests |
| `BuildOnly()` | Only builds without running |

## Requirements

- Zig 0.16.0+ installed and available in PATH (or specify path with `WithZigPath()`)
- .NET Aspire 13.2.0+
