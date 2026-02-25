# EcoData Architecture

## Overview

EcoData uses a **vertical slice / modular monolith** architecture. Each feature is isolated into its own module with well-defined boundaries, making it possible to extract features into microservices if needed. The architecture follows projection-based data access patterns and avoids Entity Framework's `.Include()` in favor of explicit `.Select()` projections.

## High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                             EcoData.AppHost                                 │
│                        (.NET Aspire Orchestration)                          │
└───────────────────────────────────┬─────────────────────────────────────────┘
                                    │ orchestrates
        ┌───────────────────────────┼───────────────────────────┐
        ▼                           ▼                           ▼
┌───────────────┐           ┌───────────────┐           ┌───────────────┐
│  PostgreSQL   │           │EcoData        │           │EcoData        │
│  (databases)  │◄──────────│.{Service}     │           │.{Service}     │
└───────────────┘           │   .Seeder     │           │   .WebApp     │
                            └───────────────┘           └───────┬───────┘
                                                                │
                                    ┌───────────────────────────┴───────┐
                                    │                                   │
                                    ▼                                   ▼
                            ┌───────────────┐               ┌───────────────┐
                            │  Features/*   │               │EcoData        │
                            │   .Api        │               │.{Service}     │
                            └───────────────┘               │.WebApp.Client │
                                                            │   (WASM)      │
                                                            └───────────────┘
```

## Project Structure

```
src/
├── Host/                                      # Shared infrastructure
│   ├── EcoData.AppHost/                      # Aspire orchestration
│   ├── EcoData.ServiceDefaults/              # Shared Aspire configuration
│   └── EcoData.Gateway/                      # YARP reverse proxy
│
├── Common/                                    # Shared contracts
│   ├── EcoData.i18n.Contracts/               # Localization interfaces
│   └── EcoData.Pagination.Contracts/         # Pagination types
│
└── {Service}/                                 # Service (e.g., AquaTrack, FaunaFinder)
    ├── EcoData.{Service}.WebApp/             # Blazor Server host
    ├── EcoData.{Service}.WebApp.Client/      # Blazor WebAssembly UI
    ├── EcoData.{Service}.Seeder/             # Database migration & seeding
    │
    └── Features/                              # Feature modules
        └── {Feature}/                         # Individual feature
            ├── EcoData.{Service}.{Feature}.Api/
            ├── EcoData.{Service}.{Feature}.Application/
            ├── EcoData.{Service}.{Feature}.Application.Client/
            ├── EcoData.{Service}.{Feature}.Contracts/
            ├── EcoData.{Service}.{Feature}.DataAccess/
            └── EcoData.{Service}.{Feature}.Database/
```

## Feature Module Structure

Each feature follows a consistent 6-project structure:

```
Features/{Feature}/
├── EcoData.{Service}.{Feature}.Api/                # Minimal API endpoints
│   └── {Feature}Endpoints.cs
│
├── EcoData.{Service}.{Feature}.Application/        # Server-side business logic
│   ├── Extensions/
│   │   └── {Feature}ApplicationConfigurator.cs
│   └── Services/
│       └── I{Service}.cs / {Service}.cs
│
├── EcoData.{Service}.{Feature}.Application.Client/ # HTTP client for WASM
│   └── {Feature}Client.cs                          # Implements same interface
│
├── EcoData.{Service}.{Feature}.Contracts/          # DTOs, requests, responses
│   ├── Dtos/                                       # (ZERO project dependencies)
│   ├── Requests/
│   ├── Responses/
│   └── Validators/
│
├── EcoData.{Service}.{Feature}.DataAccess/         # Repository implementations
│   ├── Extensions/
│   │   └── {Feature}DataAccessConfigurator.cs
│   ├── Interfaces/
│   │   └── I{Entity}Repository.cs
│   └── Repositories/
│       └── {Entity}Repository.cs
│
└── EcoData.{Service}.{Feature}.Database/           # EF Core, models, migrations
    ├── Extensions/
    │   └── {Feature}DatabaseConfigurator.cs
    ├── Models/
    │   └── {Entity}.cs                             # Contains nested EntityConfiguration
    ├── Migrations/
    └── {Feature}DbContext.cs
```

## Layer Responsibilities

### Contracts (Leaf Project - No Dependencies)

The `.Contracts` project is the foundation of each feature. It has **zero project dependencies**, making it safe to reference from Blazor WebAssembly.

```
.Contracts/
├── Dtos/           # Data transfer objects
├── Requests/       # API request models
├── Responses/      # API response models
├── Results/        # Operation result types
├── Errors/         # Error definitions
└── Validators/     # FluentValidation validators
```

**Key Rules:**
- All types are `sealed record` for immutability
- No references to other projects (can only use NuGet packages)
- Usable by both server and WASM client

### Database

Contains Entity Framework Core models, DbContext, and migrations.

**Key Patterns:**
- Each feature has its own `DbContext` and database
- Models contain nested `EntityConfiguration` classes
- Snake_case naming convention via `UseSnakeCaseNamingConvention()`
- NoTracking by default
- Uses `IDbContextFactory` pattern

```csharp
public sealed class SampleEntity
{
    public required int Id { get; set; }
    public required string Name { get; set; }
    // ...

    public sealed class EntityConfiguration : IEntityTypeConfiguration<SampleEntity>
    {
        public void Configure(EntityTypeBuilder<SampleEntity> builder)
        {
            builder.ToTable("sample_entities");
            builder.HasKey(static e => e.Id);
            // ...
        }
    }
}
```

### DataAccess

Repository implementations using projection-based queries.

**Critical Rule: NO `.Include()` - Always use `.Select()` projection**

```csharp
// DON'T DO THIS
var entity = await context.Entities
    .Include(e => e.Related)
        .ThenInclude(r => r.Nested)
    .FirstOrDefaultAsync(e => e.Id == id);
return MapToDto(entity);

// DO THIS
return await context.Entities
    .AsNoTracking()
    .Where(e => e.Id == entityId)
    .Select(static e => new EntityDetailDto(
        e.Id,
        e.Name,
        e.Related.Select(static r => new RelatedDto(
            r.Id,
            new NestedDto(r.Nested.Id, r.Nested.Code),
            r.Value
        )).ToList()
    ))
    .FirstOrDefaultAsync(cancellationToken);
```

**Benefits:**
- Single SQL query with JOINs (no N+1)
- Only requested columns are fetched
- No entity tracking overhead
- Repositories return DTOs, not entities

### Application

Server-side business logic and services. This layer orchestrates between DataAccess and external systems.

### Application.Client

HTTP client implementations for Blazor WebAssembly. Implements the same service interfaces as Application, but communicates via HTTP.

```csharp
// Server: Direct database access
public class SampleService : ISampleService { ... }

// Client: HTTP calls to API
public class SampleClient : ISampleService { ... }
```

### Api

Minimal API endpoints that expose the feature's functionality.

```csharp
public static class SampleEndpoints
{
    public static IEndpointRouteBuilder MapSampleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sample")
            .WithTags("Sample");

        group.MapGet("/{id}", GetById);
        group.MapPost("/", Create)
            .RequireAuthorization();

        return app;
    }
}
```

## Cross-Feature Communication

Features communicate through their contracts only:

```
┌─────────────────┐         ┌─────────────────┐
│    Feature A    │         │    Feature B    │
│                 │         │                 │
│  References:    │         │  References:    │
│  - Own layers   │         │  - Own layers   │
│                 │         │  - Feature A ID │
│                 │         │    (int only)   │
└─────────────────┘         └─────────────────┘
```

**Rules:**
- Cross-feature references by ID only (no navigation properties)
- No direct dependencies between feature modules
- Each feature has its own database

## Core Projects

### EcoData.{Service}.WebApp

The Blazor Server host application that:
- Hosts the Blazor WebAssembly client
- Exposes API endpoints from all features
- Handles server-side rendering
- Manages authentication cookies

### EcoData.{Service}.WebApp.Client

The Blazor WebAssembly application:
- Uses MudBlazor for UI components
- Communicates with server via HTTP clients
- Supports offline-capable scenarios

### EcoData.AppHost

.NET Aspire orchestration that manages:
- PostgreSQL database containers
- Service startup order
- Service discovery
- Development-time configuration

**Startup Order:**
1. PostgreSQL databases start
2. Seeder runs (waits for databases)
3. Server starts (waits for seeder)

### EcoData.{Service}.Seeder

Background worker that:
- Applies pending EF Core migrations
- Seeds initial data if databases are empty
- Stops automatically after completion

### EcoData.ServiceDefaults

Shared Aspire configuration:
- OpenTelemetry setup
- Health checks
- Service discovery
- HTTP resilience policies

## Dependency Graph

```
                    ┌──────────────────────┐
                    │  .Contracts (Common) │
                    │  - i18n.Contracts    │
                    │  - Pagination        │
                    └──────────┬───────────┘
                               │
         ┌─────────────────────┼─────────────────────┐
         ▼                     ▼                     ▼
┌─────────────────┐   ┌─────────────────┐   ┌─────────────────┐
│ Feature.Contracts│   │ Feature.Contracts│   │ Feature.Contracts│
│   (Feature A)   │   │   (Feature B)   │   │   (Future...)   │
└────────┬────────┘   └────────┬────────┘   └────────┬────────┘
         │                     │                     │
    ┌────┴────┐           ┌────┴────┐           ┌────┴────┐
    ▼         ▼           ▼         ▼           ▼         ▼
┌───────┐ ┌───────┐   ┌───────┐ ┌───────┐   ┌───────┐ ┌───────┐
│  App  │ │  App  │   │  App  │ │  App  │   │  App  │ │  App  │
│Client │ │Server │   │Client │ │Server │   │Client │ │Server │
└───────┘ └───┬───┘   └───────┘ └───┬───┘   └───────┘ └───┬───┘
              │                     │                     │
              ▼                     ▼                     ▼
         ┌─────────┐           ┌─────────┐           ┌─────────┐
         │DataAccess│           │DataAccess│           │DataAccess│
         └────┬────┘           └────┬────┘           └────┬────┘
              │                     │                     │
              ▼                     ▼                     ▼
         ┌─────────┐           ┌─────────┐           ┌─────────┐
         │Database │           │Database │           │Database │
         └─────────┘           └─────────┘           └─────────┘
```

## Key Architectural Decisions

### 1. Vertical Slices Over Horizontal Layers

Traditional layered architecture groups by technical concern. This architecture groups by feature/domain, making it easier to:
- Understand a feature in isolation
- Extract features to separate services
- Assign ownership to teams

### 2. Contracts as the Boundary

The `.Contracts` project defines the public API of each feature. Other features can only depend on contracts, never on implementation details.

### 3. Separate Databases Per Feature

Each feature has its own database, enabling:
- Independent scaling
- Feature isolation
- Easier microservice extraction

### 4. Dual Application Layer

The Application/Application.Client split enables:
- Same interface for server and WASM
- Type-safe API calls
- Shared validation logic

### 5. DbContextFactory Pattern

Repositories use `IDbContextFactory` instead of direct injection:

```csharp
public sealed class SampleRepository(
    IDbContextFactory<SampleDbContext> contextFactory
) : ISampleRepository
{
    public async Task<SampleDetailDto?> GetAsync(int id, CancellationToken ct)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);
        // ...
    }
}
```

**Benefits:**
- Each operation gets a fresh context
- Better for Blazor Server (long-lived connections)
- Avoids context lifetime issues

## Running the Application

```bash
# From solution root
dotnet run --project src/Host/EcoData.AppHost
```

## Adding a New Feature

1. Create the feature folder: `src/{Service}/Features/{Feature}/`
2. Create the 6 projects following the structure above
3. Register services in each `*Configurator.cs` extension
4. Add database to AppHost
5. Map endpoints in EcoData.{Service}.WebApp

## Adding Migrations

```bash
dotnet ef migrations add {MigrationName} \
    --project src/{Service}/Features/{Feature}/EcoData.{Service}.{Feature}.Database \
    --startup-project src/{Service}/EcoData.{Service}.WebApp
```
