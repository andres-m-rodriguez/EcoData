using EcoData.Organization.Contracts.Dtos;
using Microsoft.Extensions.Caching.Memory;

namespace EcoPortal.Client.Features.Organizations.Services;

public interface IOrganizationCacheService
{
    void Set(OrganizationDtoForDetail organization);
    OrganizationDtoForDetail? Get(Guid id);
    void Clear(Guid id);
}

public class OrganizationCacheService(IMemoryCache cache) : IOrganizationCacheService
{
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(10);

    private static string GetCacheKey(Guid id) => $"org:{id}";

    public void Set(OrganizationDtoForDetail organization)
    {
        var cacheKey = GetCacheKey(organization.Id);
        cache.Set(cacheKey, organization, CacheExpiration);
    }

    public OrganizationDtoForDetail? Get(Guid id)
    {
        return cache.TryGetValue<OrganizationDtoForDetail>(GetCacheKey(id), out var org)
            ? org
            : null;
    }

    public void Clear(Guid id)
    {
        var cacheKey = GetCacheKey(id);
        cache.Remove(cacheKey);
    }
}
