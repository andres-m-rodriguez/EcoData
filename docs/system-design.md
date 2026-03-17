# EcoData System Design

## Overview

EcoData is a **modular monolith** — not a traditional monolith, and not microservices. The architecture enforces strict module boundaries, separate databases per module, and no cross-module direct database access, but everything is deployed as a single unit hosted by one process.

---

## System Architecture

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                                    EcoData System                                       │
│                               (Modular Monolith Architecture)                           │
└─────────────────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                                      CLIENTS                                            │
├─────────────────────────────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────┐    ┌─────────────────────────┐    ┌───────────────────┐   │
│  │   EcoPortal.Client      │    │   External IoT Devices  │    │   Future Clients  │   │
│  │   (Blazor WebAssembly)  │    │   (Sensors)             │    │   (Mobile, etc.)  │   │
│  └───────────┬─────────────┘    └───────────┬─────────────┘    └─────────┬─────────┘   │
│              │ HTTP                         │ HTTP                       │             │
└──────────────┼──────────────────────────────┼────────────────────────────┼─────────────┘
               │                              │                            │
               ▼                              ▼                            ▼
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                                 HOST LAYER                                              │
├─────────────────────────────────────────────────────────────────────────────────────────┤
│  ┌────────────────────────────────────────────────────────────────────────────────────┐ │
│  │                        EcoPortal.Server (ASP.NET Core)                             │ │
│  │  ┌──────────────────────────────────────────────────────────────────────────────┐  │ │
│  │  │                         API Endpoints (Minimal APIs)                         │  │ │
│  │  │  /auth  /users  /organizations  /organizations/{id}/members  /sensors ...    │  │ │
│  │  └──────────────────────────────────────────────────────────────────────────────┘  │ │
│  └────────────────────────────────────────────────────────────────────────────────────┘ │
│                                                                                         │
│  ┌─────────────────────────────────┐    ┌──────────────────────────────────────────┐   │
│  │   EcoData.AppHost (Aspire)      │    │   EcoData.ServiceDefaults                │   │
│  │   - Orchestration               │    │   - OpenTelemetry                        │   │
│  │   - Service Discovery           │    │   - Health Checks                        │   │
│  │   - Azure Integration           │    │   - Standard Configurations              │   │
│  └─────────────────────────────────┘    └──────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────────────────┘
```

---

## Feature Modules

The system is composed of four feature modules, each following the same internal structure:

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                              FEATURE MODULES                                            │
├──────────────────┬──────────────────┬───────────────────┬───────────────────────────────┤
│                  │                  │                   │                               │
│  ┌───────────────▼───────────────┐  │  ┌───────────────▼───────────────┐               │
│  │        IDENTITY MODULE        │  │  │      ORGANIZATION MODULE      │               │
│  ├───────────────────────────────┤  │  ├───────────────────────────────┤               │
│  │ Identity.Api                  │  │  │ Organization.Api              │               │
│  │  └─ AuthEndpoints             │  │  │  └─ OrganizationEndpoints     │               │
│  │  └─ UserEndpoints             │  │  │  └─ MemberEndpoints           │               │
│  ├───────────────────────────────┤  │  │  └─ AccessRequestEndpoints    │               │
│  │ Identity.Application.Server   │  │  ├───────────────────────────────┤               │
│  │  └─ Business Logic            │  │  │ Organization.Application.Srv  │               │
│  ├───────────────────────────────┤  │  │  └─ Business Logic            │               │
│  │ Identity.Application.Client   │  │  ├───────────────────────────────┤               │
│  │  └─ AuthHttpClient            │  │  │ Organization.Application.Cli  │               │
│  │  └─ UserHttpClient            │  │  │  └─ OrganizationHttpClient    │               │
│  ├───────────────────────────────┤  │  │  └─ MemberHttpClient          │               │
│  │ Identity.Contracts            │  │  ├───────────────────────────────┤               │
│  │  └─ DTOs, Requests, Errors    │  │  │ Organization.Contracts        │               │
│  ├───────────────────────────────┤  │  │  └─ DTOs, Requests, Errors    │               │
│  │ Identity.DataAccess           │  │  ├───────────────────────────────┤               │
│  │  └─ UserRepository            │  │  │ Organization.DataAccess       │               │
│  ├───────────────────────────────┤  │  │  └─ OrganizationRepository    │               │
│  │ Identity.Database             │  │  │  └─ MemberRepository          │               │
│  │  └─ IdentityDbContext         │  │  ├───────────────────────────────┤               │
│  │  └─ EF Migrations             │  │  │ Organization.Database         │               │
│  └───────────────────────────────┘  │  │  └─ OrganizationDbContext     │               │
│                                     │  └─────────────────────────────────┘               │
│  ┌───────────────────────────────┐  │  ┌─────────────────────────────────┐             │
│  │        SENSORS MODULE         │  │  │       LOCATIONS MODULE          │             │
│  ├───────────────────────────────┤  │  ├─────────────────────────────────┤             │
│  │ Sensors.Api                   │  │  │ Locations.Api                   │             │
│  │  └─ SensorEndpoints           │  │  │  └─ LocationEndpoints           │             │
│  │  └─ SensorReadingEndpoints    │  │  ├─────────────────────────────────┤             │
│  │  └─ SensorHealthEndpoints     │  │  │ Locations.Contracts             │             │
│  ├───────────────────────────────┤  │  ├─────────────────────────────────┤             │
│  │ Sensors.Application.Client    │  │  │ Locations.DataAccess            │             │
│  │  └─ SensorHttpClient          │  │  ├─────────────────────────────────┤             │
│  │  └─ ReadingHttpClient         │  │  │ Locations.Database              │             │
│  ├───────────────────────────────┤  │  │  └─ LocationsDbContext          │             │
│  │ Sensors.Contracts             │  │  └─────────────────────────────────┘             │
│  ├───────────────────────────────┤  │                                                   │
│  │ Sensors.DataAccess            │  │                                                   │
│  │  └─ SensorRepository          │  │                                                   │
│  │  └─ ReadingRepository         │  │                                                   │
│  ├───────────────────────────────┤  │                                                   │
│  │ Sensors.Database              │  │                                                   │
│  │  └─ SensorsDbContext          │  │                                                   │
│  ├───────────────────────────────┤  │                                                   │
│  │ Sensors.Ingestion             │  │                                                   │
│  │  └─ Background Data Ingestion │  │                                                   │
│  └───────────────────────────────┘  │                                                   │
└─────────────────────────────────────┴───────────────────────────────────────────────────┘
```

---

## Module Internal Structure

Each module follows a consistent six-project structure:

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                          MODULE INTERNAL STRUCTURE                                      │
├─────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                         │
│   Module.Api ◄──────────────────── Endpoints (can become standalone host)              │
│       │                                                                                 │
│       ▼                                                                                 │
│   Module.Application.Server ◄───── Business Logic (THE MIGRATION SEAM)                 │
│       │                             Today: direct DB access                             │
│       │                             Future: HTTP/Service Bus calls                      │
│       ▼                                                                                 │
│   Module.DataAccess ◄───────────── Repositories (projection-based, no .Include())     │
│       │                                                                                 │
│       ▼                                                                                 │
│   Module.Database ◄─────────────── EF Core DbContext, Entities, Migrations            │
│                                                                                         │
│   Module.Contracts ◄────────────── DTOs, Requests, Errors (zero dependencies)         │
│       ▲                                                                                 │
│       │                                                                                 │
│   Module.Application.Client ◄───── HTTP Clients (consumed by Blazor WASM)              │
│                                     Uses IAsyncEnumerable<T> for streaming             │
│                                                                                         │
└─────────────────────────────────────────────────────────────────────────────────────────┘
```

### Project Responsibilities

| Project | Responsibility |
|---------|----------------|
| **Module.Api** | All endpoints defined as extension methods (`MapXEndpoints`). Hosted by the monolith's WebApp.Server, but self-contained. Can become a standalone host by adding `Program.cs`. |
| **Module.Application.Server** | Server-side services and business logic. Currently uses direct database access via repositories. Later can use HTTP or message-bus based implementations (the migration seam). |
| **Module.Application.Client** | Client-to-service communication. HTTP clients consumed by the Blazor frontend. Uses `IAsyncEnumerable<T>` for streaming collection results. |
| **Module.Contracts** | DTOs, requests, parameters, and error types. Zero project dependencies — safe for both server and WASM. All types are sealed records for immutability. |
| **Module.DataAccess** | Repository implementations. Uses `IDbContextFactory` pattern for fresh context per operation. Projection-based queries only — no `.Include()`, always `.Select()`. |
| **Module.Database** | EF Core DbContext, entity models, and migrations. Each module owns its own database. Models contain nested `EntityConfiguration` classes. Snake_case naming convention, NoTracking by default. |

---

## Common Libraries

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                              COMMON LIBRARIES                                           │
├─────────────────────────────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────┐  ┌───────────────────────┐  ┌────────────────────────────┐    │
│  │ Common.Results      │  │ Common.Pagination     │  │ Common.Http.Helpers        │    │
│  │  └─ Result<T>       │  │  └─ PagedList<T>      │  │  └─ HTTP extensions        │    │
│  │  └─ Error types     │  │  └─ Query params      │  │  └─ IAsyncEnumerable       │    │
│  └─────────────────────┘  └───────────────────────┘  └────────────────────────────┘    │
│                                                                                         │
│  ┌──────────────────────────────┐                                                       │
│  │ Common.Pagination.Blazor     │                                                       │
│  │  └─ Blazor pagination UI     │                                                       │
│  └──────────────────────────────┘                                                       │
└─────────────────────────────────────────────────────────────────────────────────────────┘
```

---

## Data Layer

Each module owns its own PostgreSQL database with no cross-module direct database access:

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                               DATA LAYER                                                │
├─────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                         │
│                           PostgreSQL Server                                             │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐    │
│  │  identity_db    │  │ organization_db │  │   sensors_db    │  │  locations_db   │    │
│  │                 │  │                 │  │                 │  │                 │    │
│  │  - users        │  │ - organizations │  │ - sensors       │  │ - locations     │    │
│  │  - roles        │  │ - members       │  │ - readings      │  │ - regions       │    │
│  │  - claims       │  │ - access_reqs   │  │ - health        │  │                 │    │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘  └─────────────────┘    │
│                                                                                         │
│      Each module owns its own database - NO cross-module direct DB access              │
│                                                                                         │
└─────────────────────────────────────────────────────────────────────────────────────────┘
```

---

## REST Resource-Based Architecture

Every layer of the stack follows the same rule: **each unit maps 1:1 to a REST resource or sub-resource and only owns operations for that resource.**

### URL Hierarchy

```
# Identity module
/auth
/users

# Organization module
/organizations
/organizations/{id}/members
/organizations/{id}/access-requests

# Sensors module
/sensors
/sensors/{id}/readings
/sensors/{id}/health

# Locations module
/locations
```

### Layer Mapping

Given a URL, you can predict the corresponding components:

| URL | Repository | Endpoint File | HTTP Client |
|-----|------------|---------------|-------------|
| `/organizations` | `OrganizationRepository` | `OrganizationEndpoints.cs` | `OrganizationHttpClient` |
| `/organizations/{id}/members` | `MemberRepository` | `MemberEndpoints.cs` | `MemberHttpClient` |
| `/sensors` | `SensorRepository` | `SensorEndpoints.cs` | `SensorHttpClient` |
| `/sensors/{id}/readings` | `ReadingRepository` | `SensorReadingEndpoints.cs` | `SensorReadingHttpClient` |

---

## Migration Path to Microservices

The architecture is explicitly designed so that any module can be promoted to a standalone microservice when traffic demands it — without a rewrite.

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                          MIGRATION PATH TO MICROSERVICES                                │
├─────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                         │
│   TODAY (Modular Monolith)              FUTURE (Microservices - when needed)           │
│   ════════════════════════              ════════════════════════════════════           │
│                                                                                         │
│   ┌─────────────────────┐               ┌─────────────────────┐                        │
│   │  Single Process     │               │  Module A Service   │ (own container)        │
│   │  ┌─────────────┐    │               └─────────────────────┘                        │
│   │  │  Module A   │    │                         │                                    │
│   │  └─────────────┘    │                         │ HTTP / Service Bus                 │
│   │  ┌─────────────┐    │                         ▼                                    │
│   │  │  Module B   │    │               ┌─────────────────────┐                        │
│   │  └─────────────┘    │               │  Module B Service   │ (own container)        │
│   │  ┌─────────────┐    │               └─────────────────────┘                        │
│   │  │  Module C   │    │                         │                                    │
│   │  └─────────────┘    │                         │ HTTP / Service Bus                 │
│   └─────────────────────┘                         ▼                                    │
│                                         ┌─────────────────────┐                        │
│                                         │  Module C Service   │ (own container)        │
│                                         └─────────────────────┘                        │
│                                                                                         │
└─────────────────────────────────────────────────────────────────────────────────────────┘
```

### Migration Steps

1. **Add `Program.cs`** to `Module.Api` — it becomes a standalone ASP.NET host
2. **Refactor `Module.Application.Server`** to use HTTP or Azure Service Bus instead of direct DB access
3. **Remove module** from the monolith's `WebApp.Server` registration
4. **Deploy** the module to its own container

Everything else — the contracts, the HTTP clients, the endpoint definitions, the database schema — stays exactly as it is.

---

## Key Architecture Principles

| Principle | Description |
|-----------|-------------|
| **Modular Monolith** | Strict module boundaries, deployed as single unit |
| **Separate DBs** | Each module owns its own PostgreSQL database |
| **REST Resource-Based** | URL hierarchy is the source of truth |
| **Migration Seam** | `Application.Server` layer can be swapped to HTTP calls |
| **No Cross-Module Queries** | Modules communicate through contracts, never direct DB |
| **Projection-Only Queries** | `.Select()` only, no `.Include()` |
| **IAsyncEnumerable Streaming** | Collection results are streamed, not loaded in memory |
| **Immutable Contracts** | All DTOs are sealed records |
| **NoTracking by Default** | EF Core contexts use no-tracking queries |

---

## Constraints Enforced Today

These rules are non-negotiable and apply now — not just when migrating:

- **Separate database per module.** Each module owns its own database on the same PostgreSQL server. No module reads from another module's database directly.
- **No cross-module direct queries.** If module A needs data from module B, it goes through `Module.Application.Server` — the same path it would take in a microservice world.
- **No `.Include()` in queries.** Repositories use projection-based queries with `.Select()` only.
- **Contracts as the boundary.** Modules expose DTOs and interfaces through `Module.Contracts`, which has zero dependencies.
- **Clear ownership.** Every table, every endpoint, every HTTP client belongs to exactly one module.

---

## When to Migrate a Module

Traffic is the signal. When a specific module's load justifies independent scaling, or when a module's deployment cycle needs to be decoupled from the rest, that's when promotion makes sense. There is no predetermined timeline — the architecture supports staying a modular monolith indefinitely if the traffic never demands otherwise.
