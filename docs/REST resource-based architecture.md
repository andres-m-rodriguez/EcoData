# REST Resource-Based Architecture

## Core Principle

Every layer of the stack follows the same rule: **each unit maps 1:1 to a REST resource or sub-resource and only owns operations for that resource.** The resource hierarchy in the URL is the source of truth — everything else mirrors it.

Each module owns its own resource hierarchy. Cross-module relationships use foreign keys, but modules don't expose each other's resources — each module is responsible for its own routes.

```
# Organization module
/organizations
/organizations/{id}/members
/organizations/{id}/access-requests

# Sensors module
/sensors
/sensors/{id}/readings
/sensors/{id}/health

# Identity module
/auth
/users
```

---

## Data Access Layer (Repositories)

Each repository owns CRUD for exactly one resource. No repository reaches into another resource's table or composes cross-resource operations.

```
# Organization module
OrganizationRepository        → organizations table
MemberRepository              → org membership records
AccessRequestRepository       → access request records

# Sensors module
SensorRepository              → sensors table
ReadingRepository             → sensor readings
SensorHealthRepository        → sensor health status
```

---

## API Endpoints

Endpoint files follow the same decomposition. Each file owns the routes for one resource:

```
# Organization module
OrganizationEndpoints       → GET/POST/PUT/DELETE /organizations
MemberEndpoints             → GET/POST/DELETE /organizations/{id}/members
AccessRequestEndpoints      → GET/POST/PUT /organizations/{id}/access-requests

# Sensors module
SensorEndpoints             → GET/POST /sensors
SensorReadingEndpoints      → GET/POST /sensors/{id}/readings
SensorHealthEndpoints       → GET/POST /sensors/{id}/health
```

---

## HTTP Clients

Same rule applies on the consuming side. Each client is a thin, focused wrapper over one resource's endpoints:

```
# Organization module
OrganizationHttpClient      → Create, Get, Update, Delete organizations
MemberHttpClient            → Get members, Add member, Remove member
AccessRequestHttpClient     → Get requests, Create request, Approve/Reject

# Sensors module
SensorHttpClient            → Get sensors, Register sensor
SensorReadingHttpClient     → Get readings, Post readings
SensorHealthHttpClient      → Get health status, Post heartbeat
```

Collection endpoints use `IAsyncEnumerable<T>` to stream results rather than loading large lists into memory at once.

---

## Project Folder Structure

Both backend and frontend use **flat folders with prefixes**. No nesting anywhere.

### Backend (flat Endpoints folder per module)

Each module has an `/Endpoints` folder. Endpoint files sit flat, named by resource.

```
# Organization module
/Organization.Api/Endpoints
  OrganizationEndpoints.cs
  MemberEndpoints.cs
  AccessRequestEndpoints.cs

# Sensors module
/Sensors.Api/Endpoints
  SensorEndpoints.cs
  SensorReadingEndpoints.cs
  SensorHealthEndpoints.cs
  SensorHealthConfigEndpoints.cs
  ReferenceDataEndpoints.cs
```

### Blazor Pages (grouped by feature)

Pages are grouped by feature area. The `@page` directive is the source of truth for the URL.

```
/Features
  /Organizations
  /OrganizationMembers
  /OrganizationAccessRequests
  /Sensors
```

---

## Blazor Components

Components do **not** strictly follow the resource-based naming rule. Component names are driven by their UI responsibility — what they render or what interaction they own — not by which resource they touch. A single component may read from multiple resources.

However, the **page routing structure** does follow the folder layout above, so the URL hierarchy is still consistent with the rest of the stack.

---

## Why This Works

- **Predictable** — given a URL, you can name the repository, endpoint file, and HTTP client without reading any code.
- **Single Responsibility** — each unit has one reason to change: its resource's schema or behavior.
- **No cross-resource leakage** — operations that touch multiple resources go through a service or command handler, never through a single repository or client.
- **Scales cleanly** — adding a new sub-resource means adding a new file at the right level, not modifying existing ones.
