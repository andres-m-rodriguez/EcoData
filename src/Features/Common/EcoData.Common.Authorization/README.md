# EcoData.Common.Authorization

One call shape for every authorization question. A scope varies in **kind** — organization or global — and each kind is answered by whichever module owns its storage.

```csharp
await auth.HasOrgPermissionAsync(orgId, "wildlife:occurrence:submit");
await auth.IsInOrgRoleAsync(orgId, "Owner");
await auth.IsInGlobalRoleAsync("GlobalAdmin");
```

Per-app permissions are modelled as an organization: FaunaFinder resolves to the org that runs it, so there is one mechanism rather than two. See §3.

## 1. Declare the keys — `<Feature>.Application`

Namespaced by feature, never by app — the org mapping is a deployment decision, the permission is a domain fact.

```csharp
namespace EcoData.Wildlife.Application;

public static class WildlifePermissions
{
    public const string ReadSpecies = "wildlife:species:read";
    public const string SubmitOccurrence = "wildlife:occurrence:submit";
    public const string VerifyOccurrence = "wildlife:occurrence:verify";
}
```

## 2. Wrap them in a typed contract — `<Feature>.Application`

One method per permission, so no call site types a key.

```csharp
public interface IWildlifePermission
{
    Task<bool> CanReadSpeciesAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default
    );

    Task<bool> CanVerifyOccurrenceAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default
    );
}
```

## 3. Hide the organization where it is fixed

FaunaFinder's contributors are members of the organization that runs it. Call sites should not know that — resolving the org here means swapping to a real app scope later touches one file.

```csharp
// FaunaFinder.Server/Options/FaunaFinderOptions.cs
public sealed class FaunaFinderOptions
{
    public const string SectionName = "FaunaFinder";

    // A slug, not a guid: organizations are created at runtime, so the id differs per
    // environment and cannot be committed as configuration. The slug is stable.
    public required string OrganizationSlug { get; set; }
}

// Resolved once at startup, then held as the id.
public sealed class FaunaFinderOrganization(Guid id)
{
    public Guid Id { get; } = id;
}

public interface IFaunaFinderPermission
{
    Task<bool> CanSubmitOccurrenceAsync(CancellationToken cancellationToken = default);
}

public sealed class FaunaFinderPermission(IAuthorization auth, FaunaFinderOrganization organization)
    : IFaunaFinderPermission
{
    public Task<bool> CanSubmitOccurrenceAsync(CancellationToken cancellationToken = default) =>
        auth.HasPermissionAsync(
            PermissionScope.Organization(organization.Id),
            WildlifePermissions.SubmitOccurrence,
            cancellationToken
        );
}
```

## 4. Implement a source per scope kind

Only the module that owns the storage writes one. Features never do.

### Organization — server

```csharp
public sealed class OrganizationPermissionSource(
    IHttpContextAccessor httpContextAccessor,
    IOrganizationPermissionService permissions,
    IOrganizationMembershipRepository memberships
) : IPermissionSource
{
    public string ScopeKind => PermissionScope.OrganizationKind;

    public async Task<bool> HasPermissionAsync(
        PermissionScope scope,
        string permission,
        CancellationToken cancellationToken = default
    )
    {
        if (!TryResolve(scope, out var userId, out var organizationId))
            return false;

        return await permissions.HasPermissionAsync(
            userId,
            organizationId,
            permission,
            cancellationToken
        );
    }

    public async Task<bool> IsInRoleAsync(
        PermissionScope scope,
        string role,
        CancellationToken cancellationToken = default
    )
    {
        if (!TryResolve(scope, out var userId, out var organizationId))
            return false;

        var membership = await memberships.GetAsync(userId, organizationId, cancellationToken);

        return string.Equals(membership?.RoleName, role, StringComparison.OrdinalIgnoreCase);
    }

    private bool TryResolve(PermissionScope scope, out Guid userId, out Guid organizationId)
    {
        userId = default;

        if (!Guid.TryParse(scope.Id, out organizationId))
            return false;

        var user = httpContextAccessor.HttpContext?.User;

        if (user is null)
            return false;

        var token = new RequestClaimToken(user);

        if (!token.IsAuthenticated)
            return false;

        userId = token.UserId.Value;

        return true;
    }
}
```

### Global — server

No scope id. Roles come off the principal; nothing is granted globally except by role.

```csharp
public sealed class GlobalPermissionSource(IHttpContextAccessor httpContextAccessor)
    : IPermissionSource
{
    public string ScopeKind => PermissionScope.GlobalKind;

    public Task<bool> HasPermissionAsync(
        PermissionScope scope,
        string permission,
        CancellationToken cancellationToken = default
    ) => Task.FromResult(false);

    public Task<bool> IsInRoleAsync(
        PermissionScope scope,
        string role,
        CancellationToken cancellationToken = default
    )
    {
        var user = httpContextAccessor.HttpContext?.User;

        return Task.FromResult(user?.IsInRole(role) ?? false);
    }
}
```

### Organization — client

Same scope kind, different transport: one fetch per scope, cached, answering every question about it.

```csharp
public sealed class OrganizationPermissionSource(IPermissionHttpClient permissionClient)
    : IPermissionSource
{
    private readonly Dictionary<Guid, Task<UserPermissionsDto>> _cache = [];

    public string ScopeKind => PermissionScope.OrganizationKind;

    public async Task<bool> HasPermissionAsync(
        PermissionScope scope,
        string permission,
        CancellationToken cancellationToken = default
    )
    {
        if (!Guid.TryParse(scope.Id, out var organizationId))
            return false;

        var permissions = await GetAsync(organizationId, cancellationToken);

        return permissions.IsGlobalAdmin || permissions.Permissions.Contains(permission);
    }

    public Task<bool> IsInRoleAsync(
        PermissionScope scope,
        string role,
        CancellationToken cancellationToken = default
    ) => Task.FromResult(false);

    // Caches the Task, not the result, so concurrent callers share one request.
    private Task<UserPermissionsDto> GetAsync(
        Guid organizationId,
        CancellationToken cancellationToken
    )
    {
        if (_cache.TryGetValue(organizationId, out var cached))
            return cached;

        var task = FetchAsync(organizationId, cancellationToken);
        _cache[organizationId] = task;

        return task;
    }
}
```

## 5. Register — host

Each host registers only the kinds it uses.

```csharp
// EcoPortal.Server
builder.Services.AddAuthorization();
builder.Services.AddPermissionSource<OrganizationPermissionSource>();
builder.Services.AddPermissionSource<GlobalPermissionSource>();

// EcoPortal.Client
builder.Services.AddAuthorization();
builder.Services.AddPermissionSource<OrganizationPermissionSource>();   // the HTTP one
```

FaunaFinder additionally resolves its organization once, at startup:

```csharp
// FaunaFinder.Server/Program.cs
builder.Services.Configure<FaunaFinderOptions>(
    builder.Configuration.GetSection(FaunaFinderOptions.SectionName)
);
builder.Services.AddAuthorization();
builder.Services.AddPermissionSource<OrganizationPermissionSource>();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var slug = scope
        .ServiceProvider.GetRequiredService<IOptions<FaunaFinderOptions>>()
        .Value.OrganizationSlug;

    var organization =
        await scope
            .ServiceProvider.GetRequiredService<IOrganizationRepository>()
            .GetBySlugAsync(slug)
        ?? throw new InvalidOperationException(
            $"FaunaFinder is configured for organization '{slug}', which does not exist."
        );

    app.Services.GetRequiredService<FaunaFinderOrganization>();   // registered from organization.Id
}
```

```json
{ "FaunaFinder": { "OrganizationSlug": "intermetro" } }
```

## 6. Use it

```csharp
// Endpoint
group
    .MapPost(
        "/{id:guid}/verify",
        async Task<Results<NoContent, ForbidHttpResult, NotFound>> (
            Guid id,
            IWildlifePermission wildlife,
            IOccurrenceRepository repository,
            CancellationToken ct
        ) =>
        {
            var occurrence = await repository.GetByIdAsync(id, ct);

            if (occurrence is null)
                return TypedResults.NotFound();

            if (!await wildlife.CanVerifyOccurrenceAsync(occurrence.OrganizationId, ct))
                return TypedResults.Forbid();

            await repository.VerifyAsync(id, ct);

            return TypedResults.NoContent();
        }
    )
    .RequireAuthorization();
```

```razor
@inject IFaunaFinderPermission Permission

@if (_canSubmit)
{
    <MudButton OnClick="SubmitAsync">Submit sighting</MudButton>
}

@code {
    private bool _canSubmit;

    protected override async Task OnInitializedAsync() =>
        _canSubmit = await Permission.CanSubmitOccurrenceAsync();
}
```

## Rules

- Adding a scope kind means adding a source. An unregistered kind **throws** naming the missing kind — it never denies silently, because a silent `false` is indistinguishable from a correct denial.
- Two sources claiming one kind throws when `IAuthorization` is first resolved.
- Client answers shape UI only. They can be more permissive than the server, which sees rules the browser cannot. The endpoint check is the enforcement.
- Permission keys appear in `<Feature>.Application` and inside sources. Never at a call site.
- FaunaFinder's organization is configuration, not a constant. It becomes wrong the day a second institution contributes — at which point the fix is a new scope kind behind `IFaunaFinderPermission`, and no call site changes.

The generic form stays available for a scope kind with no shorthand:

```csharp
await auth.HasPermissionAsync(PermissionScope.Custom("project", projectId), "project:publish");
```
