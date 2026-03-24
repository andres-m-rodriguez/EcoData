# EcoData

Environmental monitoring platform built as a modular monolith.

## Overview

EcoData aggregates and manages environmental datasets including:

- **Sensors** - Real-time water quality and environmental monitoring
- **Organizations** - Multi-tenant organization management
- **Locations** - Geographic data for Mexican states and municipalities

## Architecture

Modular monolith with strict module boundaries and separate databases per module. Each module can be promoted to a microservice when traffic demands it.

See [docs/architecture.md](docs/architecture.md) and [docs/system-design.md](docs/system-design.md) for details.

## Getting Started

```bash
dotnet run --project src/Host/EcoData.AppHost
```

## Structure

```
src/
├── Host/
│   ├── EcoData.AppHost/         # Aspire orchestrator
│   └── EcoData.ServiceDefaults/ # Shared configuration
├── Apps/
│   └── EcoPortal/               # Main web application
│       ├── EcoPortal.Server/    # ASP.NET Core host
│       └── EcoPortal.Client/    # Blazor WASM
└── Features/
    ├── Identity/                # Authentication & users
    ├── Organization/            # Organizations & members
    ├── Sensors/                 # Sensors & readings
    ├── Locations/               # Geographic data
    └── Common/                  # Shared libraries
```

## License

TBD
