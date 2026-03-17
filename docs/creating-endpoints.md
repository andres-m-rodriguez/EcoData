# Creating API Endpoints

Guidelines and best practices for creating API endpoints in EcoData.

---

## Where to Put Endpoints

Endpoints live in the `Module.Api` project for each feature module:

```
/src/Features/{Module}/{Module}.Api/
├── Endpoints/
│   ├── {Resource}Endpoints.cs
│   └── {SubResource}Endpoints.cs
└── {Module}ApiExtensions.cs
```

Each file owns endpoints for **one resource**. This follows the REST resource-based architecture — given a URL, you can predict the file name.

| URL | File |
|-----|------|
| `/api/sensors` | `SensorEndpoints.cs` |
| `/api/sensors/{id}/readings` | `SensorReadingEndpoints.cs` |
| `/api/organizations/{id}/members` | `MemberEndpoints.cs` |

---

## Naming Conventions

### Files and Classes

- File: `{Resource}Endpoints.cs` (plural resource name)
- Class: `public static class {Resource}Endpoints`

### Route Names

Use `WithName()` for every endpoint. Pattern: `{Verb}{Resource}{Action}`

| Operation | Name |
|-----------|------|
| List all | `GetSensors` |
| Get by ID | `GetSensorById` |
| Create | `CreateSensor` or `RegisterSensor` |
| Update | `UpdateSensor` |
| Delete | `DeleteSensor` |
| Custom action | `ApproveSensorRequest` |

---

## Route Design

### Use MapGroup

Always group related endpoints together:

- Keeps routes organized
- Allows shared authorization
- Groups endpoints in Swagger

### Nested Resources

Sub-resources go under their parent:

| Resource | Route |
|----------|-------|
| Organization members | `/api/organizations/{id}/members` |
| Sensor readings | `/api/sensors/{id}/readings` |
| Access requests | `/api/organizations/{id}/access-requests` |

### User-Scoped Routes

For resources where users access their own data, use `/api/me/`:

| Route | Purpose |
|-------|---------|
| `/api/me/access-requests` | User's own access requests |
| `/api/me/organizations` | Organizations user belongs to |

---

## Request Handling

### Query Parameters

Use `[AsParameters]` with a parameters record for list endpoints. Parameters should inherit from `CursorParameters` for pagination.

### Request Bodies

Define request DTOs in the Contracts project. Keep them focused — different DTOs for create vs update if needed.

### Validation

Validate requests using FluentValidation. Return `ValidationProblem` with grouped errors by field name.

---

## Response Patterns

### Use Typed Results

Always declare all possible return types explicitly. This documents the API and generates accurate OpenAPI specs.

Common responses:

| Result | When to use |
|--------|-------------|
| `Ok<T>` | Successful GET or PUT |
| `Created<T>` | Successful POST (include location) |
| `NoContent` | Successful DELETE |
| `NotFound` | Resource doesn't exist |
| `Conflict` | Business rule violation (duplicate, invalid state) |
| `ValidationProblem` | Request validation failed |
| `Unauthorized` | Not authenticated |
| `Forbid` | Authenticated but no permission |

### Conflict Messages

When returning `Conflict`, include a clear message explaining why:

- "A sensor with this external ID already exists"
- "You are already a member of this organization"
- "You have a pending access request"

---

## Authorization

### Group-Level Authorization

Apply `RequireAuthorization()` to the group for endpoints that need authentication.

### Public Endpoints

Use `AllowAnonymous()` to override group-level auth for specific endpoints that should be public.

### Permission Checks

For fine-grained permissions:

1. Extract the user from claims
2. Check permission via the permission service
3. Return `Forbid` if not authorized

Always check permissions — don't assume the UI will hide unauthorized actions.

### Multiple Auth Schemes

Some endpoints support different auth methods:

- User authentication (cookies/session)
- API key authentication
- Sensor JWT authentication

Configure the appropriate scheme for each endpoint type.

---

## Data Access

### Use Repositories

Never access the database directly from endpoints. Always go through the repository in the DataAccess project.

### Inject Dependencies

Dependencies are injected directly into endpoint handlers. Keep handlers focused — complex logic should live in services.

### Streaming Lists

List endpoints should return `IAsyncEnumerable<T>` for streaming. This is handled by the repository.

---

## Error Handling

### Business Rule Violations

Check business rules and return appropriate errors:

- Duplicate check → `Conflict`
- Invalid state transition → `Conflict` or `BadRequest`
- Missing related entity → `NotFound`
- No permission → `Forbid`

### Batch Operations

For batch endpoints, process all items and return a summary:

- Total count
- Success count
- Error count
- List of error messages

Don't fail the entire batch for individual item errors.

---

## Registration

### Extension Method Pattern

Each endpoint file has one public method: `Map{Resource}Endpoints`

The module's `ApiExtensions.cs` aggregates all endpoints:

```
MapOrganizationApiEndpoints()
  ├── MapOrganizationEndpoints()
  ├── MapMemberEndpoints()
  └── MapAccessRequestEndpoints()
```

This single call registers all endpoints for the module.

---

## Checklist

Before considering an endpoint done:

- [ ] File placed in correct module's `Endpoints/` folder
- [ ] Named following conventions
- [ ] Route follows REST resource hierarchy
- [ ] All return types declared explicitly
- [ ] Request validation implemented
- [ ] Authorization configured appropriately
- [ ] Permission checks in place (if needed)
- [ ] Clear error messages for failures
- [ ] Uses repository, not direct DB access
- [ ] Registered in module's ApiExtensions
