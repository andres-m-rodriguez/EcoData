# EcoData.Common.Authorization

One verb for every authorization question. Scope lives in the permission's *type*, so the
compiler enforces the call shape: an organization permission cannot be checked without an
organization id, and a global one cannot be given one.

```csharp
await auth.HasAsync(WildlifePermissions.VerifyOccurrence, occurrence.OrganizationId, ct);
await auth.HasAsync(OrganizationPermissions.Create, ct);
```

UI that renders many checks synchronously loads a snapshot once instead of asking
one question at a time — every grant the caller holds in the organization, plus
`IsGlobalAdmin`:

```csharp
var grants = await auth.GrantsAsync(organizationId, ct);
```

A snapshot shapes UI only; the endpoint's `HasAsync` remains the enforcement.

## 1. Declare the keys — `<Feature>.Contracts`

Raw strings, zero dependencies — this is what the wire and the membership storage speak.

```csharp
namespace EcoData.Wildlife.Contracts;

public static class Permissions
{
    public static class Occurrence
    {
        public const string Submit = "wildlife:occurrence:submit";
        public const string Verify = "wildlife:occurrence:verify";
    }
}
```

## 2. Declare the typed permissions — `<Feature>.Application`

The type picks the scope. Call sites reference these fields; nobody types a key twice.

```csharp
namespace EcoData.Wildlife.Application;

public static class WildlifePermissions
{
    public static readonly OrgPermission SubmitOccurrence = new(Permissions.Occurrence.Submit);

    public static readonly OrgPermission VerifyOccurrence = new(Permissions.Occurrence.Verify);
}
```

A global action granted by holding a role rather than a stored grant is declared with the
role — the call site still names the action, never the role:

```csharp
public static readonly GlobalRolePermission Create =
    new(Permissions.Organization.Create, GlobalRoles.GlobalAdmin);
```

## 3. Implement a source per scope type

Only the module that owns the storage writes one. Organization implements
`IOrganizationPermissionSource` from membership storage; Identity implements
`IGlobalPermissionSource` from global roles. Features never implement sources.

The same interfaces get HTTP-backed implementations in `.Application.Client` projects, so
Blazor pages keep the same call shape with different wiring: Organization implements
`OrganizationPermissionHttpSource` (per-organization cached fetch of the caller's grants),
and EcoPortal.Client registers it with `AddOrganizationPermissionHttpSource()`. An auth
change calls its `InvalidateCache()` — cached grants belong to one user.

## 4. Register — host

Each host registers only the scope types it uses.

```csharp
builder.Services.AddPermissions();
builder.Services.AddOrganizationPermissionSource();
builder.Services.AddGlobalPermissionSource();
```

## 5. Use it

The shape for the future occurrence endpoints — the org id comes off the loaded entity:

```csharp
group
    .MapPost(
        "/{id:guid}/verify",
        async Task<Results<NoContent, ForbidHttpResult, NotFound>> (
            Guid id,
            IAuthorization auth,
            IOccurrenceRepository repository,
            CancellationToken ct
        ) =>
        {
            var occurrence = await repository.GetByIdAsync(id, ct);

            if (occurrence is null)
                return TypedResults.NotFound();

            if (!await auth.HasAsync(WildlifePermissions.VerifyOccurrence, occurrence.OrganizationId, ct))
                return TypedResults.Forbid();

            await repository.VerifyAsync(id, ct);

            return TypedResults.NoContent();
        }
    )
    .RequireAuthorization();
```

## Rules

- A check against a scope type with no registered source **throws**, naming the missing
  source and the permission key. It never denies silently — a silent `false` is
  indistinguishable from a correct denial.
- A plain `GlobalPermission` throws until a global grant store exists. Declare role-backed
  actions as `GlobalRolePermission`.
- Client answers shape UI only. They can be more permissive than the server, which sees
  rules the browser cannot. The endpoint check is the enforcement.
- Permission keys appear in `<Feature>.Contracts` and inside sources. Never at a call site.
- Org roles are not part of the API. Model grants, not role checks — a role is how an
  organization bundles permissions, not something an endpoint asks about.
- One typed source interface per scope, one owning module. Re-registering a source is an
  ordinary DI override, not an error.
