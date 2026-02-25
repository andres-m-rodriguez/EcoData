# EcoData

A monorepo for ecological and environmental data.

## Overview

This repository aggregates and manages various environmental datasets and research, including:

- **Water Sensors** - Real-time and historical water quality and level monitoring data
- **Fauna** - Wildlife tracking, species data, and biodiversity research
- **Research Data** - Scientific studies and environmental research datasets

## Repository Structure

```
EcoData/
├── src/
│   ├── Host/
│   │   ├── EcoData.AppHost/           # Aspire orchestrator
│   │   ├── EcoData.ServiceDefaults/   # Shared service configuration
│   │   └── EcoData.Gateway/           # YARP reverse proxy
│   └── AquaTrack/
│       ├── EcoData.AquaTrack.WebApp/        # Blazor server host
│       └── EcoData.AquaTrack.WebApp.Client/ # WASM client
└── EcoData.slnx
```

## Getting Started

```bash
dotnet run --project src/Host/EcoData.AppHost
```

## License

TBD
