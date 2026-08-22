using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Components.Endpoints;

namespace EcoPortal.Server.Authorization;

// Pages run in the browser (InteractiveWebAssembly, no prerender), yet [Authorize] and
// [OrganizationPermission] on a page become endpoint metadata that the server would enforce
// on the initial document request — answering a typed-in URL with a bare 401/403 before the
// app ever loads. The document is only the app shell; every API call still enforces its own
// authorization. Serve it and let the client router send the person to login or show that
// their role lacks the permission.
public sealed class ClientRoutedPageAuthorizationHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _default = new();

    public Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult
    )
    {
        if (
            !authorizeResult.Succeeded
            && context.GetEndpoint()?.Metadata.GetMetadata<ComponentTypeMetadata>() is not null
        )
        {
            return next(context);
        }

        return _default.HandleAsync(next, context, policy, authorizeResult);
    }
}
