# Why Modular Monolith (and How We Migrate When Ready)

## The Decision

EcoData is a **modular monolith** — not a traditional monolith, and not microservices. The architecture enforces strict module boundaries, separate databases per module, and no cross-module direct database access, but everything is deployed as a single unit hosted by one process.

The decision is not permanent. The architecture is explicitly designed so that any module can be promoted to a standalone microservice when traffic demands it — without a rewrite.

---

## Why Not Microservices Now

Microservices solve real problems, but those problems need to actually exist first.

**Cost.** Running each module as its own service means separate hosting costs for each one. At current scale, that overhead adds cost without adding value. One process, one deployment, one bill.

**Team size.** Microservices are largely a team-scaling solution — they let independent teams own and deploy services without stepping on each other. With one developer, the coordination overhead that microservices eliminate doesn't exist, but the operational complexity they introduce does.

**Not at scale yet.** Microservices let you scale individual services independently based on traffic. That's a real advantage — when you have traffic patterns that justify it. Paying for that flexibility before the traffic exists is premature optimization.

**Complexity has a cost.** Service discovery, network failures, distributed tracing, eventual consistency, inter-service authentication — these are all real problems microservices introduce that a monolith doesn't have. Taking on that complexity before it's necessary slows down development without a corresponding benefit.

---

## Why Not a Traditional Monolith Either

A traditional monolith shares everything — one database, one codebase with no enforced boundaries, modules that reach directly into each other's tables. That works at small scale but becomes painful quickly: schema changes have unpredictable blast radius, ownership is unclear, and migrating to microservices later requires untangling a mess.

The modular monolith avoids that by enforcing the same boundaries microservices would have, while keeping the deployment simple.

---

## The Module Structure

Each module is a set of six projects with fixed responsibilities:

```
Module.Api
  └── All endpoints defined as extension methods (MapXEndpoints)
      Hosted by the monolith's WebApp.Server, but self-contained
      Can become a standalone host by adding Program.cs

Module.Application.Server
  └── Server-side services and business logic
      Currently: direct database access via repositories
      Later: HTTP or message-bus based implementations (the migration seam)

Module.Application.Client
  └── Client-to-service communication
      HTTP clients consumed by the Blazor frontend
      Uses IAsyncEnumerable<T> for streaming collection results

Module.Contracts
  └── DTOs, requests, parameters, and error types
      Zero project dependencies — safe for both server and WASM
      All types are sealed records for immutability

Module.DataAccess
  └── Repository implementations
      Uses IDbContextFactory pattern for fresh context per operation
      Projection-based queries only — no .Include(), always .Select()
      Takes parameters and returns DTOs/errors from the Contracts layer
      Interface mirrors the HTTP client in shape — same inputs, same outputs (not a shared interface)

Module.Database
  └── EF Core DbContext, entity models, and migrations
      Each module owns its own database
      Models contain nested EntityConfiguration classes
      Snake_case naming convention, NoTracking by default
```

**What makes this structure significant** is that `Module.Application.Server` is the seam. Today it talks directly to `Module.DataAccess` and `Module.Database`. When a module needs to become its own service, `Module.Application.Server` gets refactored to call over the network instead — and nothing else changes. The contracts, clients, endpoints, and database are all already self-contained.

---

## The Migration Path

When a module's traffic justifies running it independently, the migration is a promotion, not a rewrite:

1. Add a `Program.cs` to `Module.Api` — it becomes a standalone ASP.NET host
2. Refactor `Module.Application.Server` to use HTTP or Azure Service Bus instead of direct DB access
3. Remove that module from the monolith's `WebApp.Server` registration
4. Deploy the module to its own container

Everything else — the contracts, the HTTP clients, the endpoint definitions, the database schema — stays exactly as it is. The module was already behaving like a microservice in terms of boundaries. Deployment is the only thing that changes.

---

## Constraints Enforced Today

Even as a monolith, these rules are non-negotiable and apply now — not just when we migrate:

- **Separate database per module.** Each module owns its own database on the same PostgreSQL server. No module reads from another module's database directly.
- **No cross-module direct queries.** If module A needs data from module B, it goes through `Module.Application.Server` — the same path it would take in a microservice world.
- **No .Include() in queries.** Repositories use projection-based queries with `.Select()` only. This enforces that data fetching is deliberate and bounded, never accidentally pulling in related data from outside the module's domain.
- **Contracts as the boundary.** Modules expose DTOs and interfaces through `Module.Contracts`, which has zero dependencies and is safe to reference from both server and WASM. The implementation is always hidden behind that boundary.
- **Clear ownership.** Every table, every endpoint, every HTTP client belongs to exactly one module. No shared tables, no shared repositories.

These constraints mean the architecture is already microservice-compatible — it's just collocated for now.

---

## When to Migrate a Module

Traffic is the signal. When a specific module's load justifies independent scaling, or when a module's deployment cycle needs to be decoupled from the rest, that's when promotion makes sense. There is no predetermined timeline — the architecture supports staying a modular monolith indefinitely if the traffic never demands otherwise.
