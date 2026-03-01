using System.Runtime.CompilerServices;
using EcoData.Locations.Contracts.Dtos;
using EcoData.Locations.Contracts.Parameters;
using EcoData.Locations.Database;
using EcoData.Locations.DataAccess.Interfaces;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace EcoData.Locations.DataAccess.Repositories;

public sealed class MunicipalityRepository(IDbContextFactory<LocationsDbContext> contextFactory)
    : IMunicipalityRepository
{
    private static readonly GeoJsonWriter GeoJsonWriter = new();

    public IAsyncEnumerable<MunicipalityDtoForList> GetMunicipalitiesAsync(
        MunicipalityParameters parameters,
        CancellationToken cancellationToken = default
    )
    {
        return GetMunicipalitiesInternalAsync(parameters, cancellationToken);
    }

    private async IAsyncEnumerable<MunicipalityDtoForList> GetMunicipalitiesInternalAsync(
        MunicipalityParameters parameters,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var query = context.Municipalities.AsNoTracking().AsQueryable();

        if (parameters.StateId.HasValue)
        {
            query = query.Where(m => m.StateId == parameters.StateId.Value);
        }
        else if (!string.IsNullOrWhiteSpace(parameters.StateCode))
        {
            var normalizedCode = parameters.StateCode.ToUpperInvariant();
            query = query.Where(m => m.State!.Code == normalizedCode);
        }

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var search = parameters.Search.Trim().ToLower();
            query = query.Where(m => m.Name.ToLower().Contains(search));
        }

        if (parameters.Cursor.HasValue)
        {
            query = query.Where(m => m.Id > parameters.Cursor.Value);
        }

        await foreach (var municipality in query
            .OrderBy(m => m.Id)
            .Take(parameters.PageSize + 1)
            .Select(static m => new MunicipalityDtoForList(
                m.Id,
                m.Name,
                m.GeoJsonId,
                m.CountyFipsCode,
                m.CentroidLatitude,
                m.CentroidLongitude
            ))
            .AsAsyncEnumerable()
            .WithCancellation(cancellationToken))
        {
            yield return municipality;
        }
    }

    public async Task<MunicipalityDtoForDetail?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Municipalities
            .Include(m => m.State)
            .Where(m => m.Id == id)
            .Select(m => new MunicipalityDtoForDetail(
                m.Id,
                m.StateId,
                m.State!.Name,
                m.State.Code,
                m.Name,
                m.GeoJsonId,
                m.CountyFipsCode,
                m.CentroidLatitude,
                m.CentroidLongitude,
                m.CreatedAt
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<MunicipalityDtoForDetail?> GetByGeoJsonIdAsync(
        string geoJsonId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Municipalities
            .Include(m => m.State)
            .Where(m => m.GeoJsonId == geoJsonId)
            .Select(m => new MunicipalityDtoForDetail(
                m.Id,
                m.StateId,
                m.State!.Name,
                m.State.Code,
                m.Name,
                m.GeoJsonId,
                m.CountyFipsCode,
                m.CentroidLatitude,
                m.CentroidLongitude,
                m.CreatedAt
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<MunicipalityDtoForDetail?> GetByPointAsync(
        decimal latitude,
        decimal longitude,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var point = new Point((double)longitude, (double)latitude) { SRID = 4326 };

        return await context.Municipalities
            .Include(m => m.State)
            .Where(m => m.Boundary != null && m.Boundary.Contains(point))
            .Select(m => new MunicipalityDtoForDetail(
                m.Id,
                m.StateId,
                m.State!.Name,
                m.State.Code,
                m.Name,
                m.GeoJsonId,
                m.CountyFipsCode,
                m.CentroidLatitude,
                m.CentroidLongitude,
                m.CreatedAt
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MunicipalityDtoForGeoJson>> GetGeoJsonByStateCodeAsync(
        string stateCode,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var normalizedCode = stateCode.ToUpperInvariant();

        var municipalities = await context.Municipalities
            .Where(m => m.State!.Code == normalizedCode)
            .OrderBy(m => m.Name)
            .Select(m => new
            {
                m.Id,
                m.Name,
                m.GeoJsonId,
                m.CentroidLatitude,
                m.CentroidLongitude,
                m.Boundary
            })
            .ToListAsync(cancellationToken);

        return municipalities.Select(m => new MunicipalityDtoForGeoJson(
            m.Id,
            m.Name,
            m.GeoJsonId,
            m.CentroidLatitude,
            m.CentroidLongitude,
            m.Boundary != null ? GeoJsonWriter.Write(m.Boundary) : null
        )).ToList();
    }
}
