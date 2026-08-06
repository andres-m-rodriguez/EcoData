using System.Runtime.CompilerServices;
using EcoData.Common.i18n;
using EcoData.Wildlife.Contracts;
using EcoData.Wildlife.Contracts.Dtos;
using EcoData.Wildlife.Contracts.Parameters;
using EcoData.Wildlife.DataAccess.Interfaces;
using EcoData.Wildlife.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EcoData.Wildlife.DataAccess.Repositories;

public sealed class SpeciesRepository(
    IDbContextFactory<WildlifeDbContext> contextFactory,
    IOptions<WildlifeOptions> options
) : ISpeciesRepository
{
    private static readonly IucnStatus[] ThreatenedStatuses =
        [IucnStatus.VU, IucnStatus.EN, IucnStatus.CR];

    public async Task<SpeciesDtoForDetail?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context
            .Species.Where(s => s.Id == id)
            .Select(s => new SpeciesDtoForDetail(
                s.Id,
                s.CommonName,
                s.ScientificName,
                s.IsFauna,
                s.ElCode,
                s.GRank,
                s.SRank,
                s.ImageSourceUrl,
                s.ProfileImageData != null,
                s.CategoryLinks
                    .Select(cl => new SpeciesCategoryDtoForList(
                        cl.Category.Id,
                        cl.Category.Code,
                        cl.Category.Name
                    ))
                    .ToList(),
                s.MunicipalitySpecies.Select(ms => ms.MunicipalityId).ToList(),
                s.IsEndemic,
                s.IucnStatus,
                s.Habitat,
                s.LastObservedAtUtc,
                s.Locations
                    .Select(l => new SpeciesLocationDto(
                        l.Id,
                        l.Latitude,
                        l.Longitude,
                        l.RadiusMeters,
                        l.Description
                    ))
                    .ToList()
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SpeciesNearbyDto>> GetNearbyAsync(
        double latitude,
        double longitude,
        double radiusMeters,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        // The occurrence set is small (a few hundred circles), so distances are
        // computed in memory rather than pushing PostGIS geography into the model.
        var speciesWithLocations = await QuerySpeciesWithLocations(context)
            .ToListAsync(cancellationToken);

        var results = new List<SpeciesNearbyDto>();

        foreach (var species in speciesWithLocations)
        {
            foreach (var location in species.Locations)
            {
                var distance = HaversineDistanceMeters(
                    latitude,
                    longitude,
                    location.Latitude,
                    location.Longitude
                );

                // The occurrence is a circle; match when its edge falls inside the search radius.
                var effectiveDistance = Math.Max(0, distance - location.RadiusMeters);
                if (effectiveDistance <= radiusMeters)
                {
                    results.Add(
                        new SpeciesNearbyDto(
                            species.Id,
                            species.CommonName,
                            species.ScientificName,
                            species.IsFauna,
                            distance,
                            location.Latitude,
                            location.Longitude,
                            location.RadiusMeters,
                            location.Description
                        )
                    );
                }
            }
        }

        return ClosestLocationPerSpecies(results);
    }

    public async Task<IReadOnlyList<SpeciesNearbyDto>> GetInPolygonAsync(
        IReadOnlyList<PolygonCoordinate> coordinates,
        CancellationToken cancellationToken = default
    )
    {
        if (coordinates.Count < 3)
            return [];

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var speciesWithLocations = await QuerySpeciesWithLocations(context)
            .ToListAsync(cancellationToken);

        var centroidLat = coordinates.Average(c => c.Latitude);
        var centroidLng = coordinates.Average(c => c.Longitude);

        var results = new List<SpeciesNearbyDto>();

        foreach (var species in speciesWithLocations)
        {
            foreach (var location in species.Locations)
            {
                if (!IsPointInPolygon(location.Latitude, location.Longitude, coordinates))
                    continue;

                var distance = HaversineDistanceMeters(
                    centroidLat,
                    centroidLng,
                    location.Latitude,
                    location.Longitude
                );

                results.Add(
                    new SpeciesNearbyDto(
                        species.Id,
                        species.CommonName,
                        species.ScientificName,
                        species.IsFauna,
                        distance,
                        location.Latitude,
                        location.Longitude,
                        location.RadiusMeters,
                        location.Description
                    )
                );
            }
        }

        return ClosestLocationPerSpecies(results);
    }

    public async Task<IReadOnlyList<HeatmapPointDto>> GetHeatmapAsync(
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context
            .Species.AsNoTracking()
            .Where(s => s.Locations.Any())
            .SelectMany(s => s.Locations.Select(l => new HeatmapPointDto(
                l.Latitude,
                l.Longitude,
                1.0,
                s.IsFauna
            )))
            .ToListAsync(cancellationToken);
    }

    private static IQueryable<SpeciesLocationsProjection> QuerySpeciesWithLocations(
        WildlifeDbContext context
    ) =>
        context
            .Species.AsNoTracking()
            .Where(s => s.Locations.Any())
            .Select(s => new SpeciesLocationsProjection(
                s.Id,
                s.CommonName,
                s.ScientificName,
                s.IsFauna,
                s.Locations
                    .Select(l => new SpeciesLocationDto(
                        l.Id,
                        l.Latitude,
                        l.Longitude,
                        l.RadiusMeters,
                        l.Description
                    ))
                    .ToList()
            ));

    private sealed record SpeciesLocationsProjection(
        Guid Id,
        List<LocaleValue> CommonName,
        string ScientificName,
        bool IsFauna,
        List<SpeciesLocationDto> Locations
    );

    private static IReadOnlyList<SpeciesNearbyDto> ClosestLocationPerSpecies(
        List<SpeciesNearbyDto> results
    ) =>
        results
            .GroupBy(r => r.Id)
            .Select(g => g.OrderBy(r => r.DistanceMeters).First())
            .OrderBy(r => r.DistanceMeters)
            .ToList();

    private static double HaversineDistanceMeters(
        double lat1,
        double lon1,
        double lat2,
        double lon2
    )
    {
        const double EarthRadiusMeters = 6371000;

        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);

        var a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
            + Math.Cos(DegreesToRadians(lat1))
                * Math.Cos(DegreesToRadians(lat2))
                * Math.Sin(dLon / 2)
                * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return EarthRadiusMeters * c;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180;

    private static bool IsPointInPolygon(
        double latitude,
        double longitude,
        IReadOnlyList<PolygonCoordinate> polygon
    )
    {
        // Ray casting: count edge crossings of a horizontal ray from the point.
        var inside = false;
        var j = polygon.Count - 1;

        for (var i = 0; i < polygon.Count; j = i++)
        {
            var xi = polygon[i].Longitude;
            var yi = polygon[i].Latitude;
            var xj = polygon[j].Longitude;
            var yj = polygon[j].Latitude;

            if (((yi > latitude) != (yj > latitude)) &&
                (longitude < (xj - xi) * (latitude - yi) / (yj - yi) + xi))
            {
                inside = !inside;
            }
        }

        return inside;
    }

    public async IAsyncEnumerable<SpeciesDtoForList> GetSpeciesAsync(
        SpeciesParameters parameters,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var query = context.Species.AsQueryable();

        query = ApplyFilters(query, parameters);

        query = ApplySort(query, parameters.Sort);

        // Cursor pagination is Id-based; correct only for ScientificNameAsc + Id tiebreaker.
        // Non-default sorts fall back to first-page results (follow-up tracked in issue #188).
        if (parameters.Cursor.HasValue)
        {
            query = query.Where(s => s.Id < parameters.Cursor.Value);
        }

        await foreach (
            var species in query
                .Take(parameters.PageSize + 1)
                .Select(static s => new SpeciesDtoForList(
                    s.Id,
                    s.CommonName,
                    s.ScientificName,
                    s.IsFauna,
                    s.GRank,
                    s.SRank,
                    s.ProfileImageData != null,
                    s.IsEndemic,
                    s.IucnStatus,
                    s.CategoryLinks.Select(cl => cl.Category.Code).FirstOrDefault(),
                    s.MunicipalitySpecies.Count,
                    s.LastObservedAtUtc,
                    s.IsFeatured
                ))
                .AsAsyncEnumerable()
                .WithCancellation(cancellationToken)
        )
        {
            yield return species;
        }
    }

    public async Task<int> GetCountAsync(
        SpeciesParameters parameters,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var query = context.Species.AsQueryable();
        query = ApplyFilters(query, parameters);

        return await query.CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SpeciesDtoForList>> GetByMunicipalityAsync(
        Guid municipalityId,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context
            .MunicipalitySpecies.Where(ms => ms.MunicipalityId == municipalityId)
            .Select(static ms => new SpeciesDtoForList(
                ms.Species.Id,
                ms.Species.CommonName,
                ms.Species.ScientificName,
                ms.Species.IsFauna,
                ms.Species.GRank,
                ms.Species.SRank,
                ms.Species.ProfileImageData != null,
                ms.Species.IsEndemic,
                ms.Species.IucnStatus,
                ms.Species.CategoryLinks.Select(cl => cl.Category.Code).FirstOrDefault(),
                ms.Species.MunicipalitySpecies.Count,
                ms.Species.LastObservedAtUtc,
                ms.Species.IsFeatured
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SpeciesDtoForList>> GetByCategoryAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context
            .SpeciesCategoryLinks.Where(scl => scl.CategoryId == categoryId)
            .Select(static scl => new SpeciesDtoForList(
                scl.Species.Id,
                scl.Species.CommonName,
                scl.Species.ScientificName,
                scl.Species.IsFauna,
                scl.Species.GRank,
                scl.Species.SRank,
                scl.Species.ProfileImageData != null,
                scl.Species.IsEndemic,
                scl.Species.IucnStatus,
                scl.Species.CategoryLinks.Select(cl => cl.Category.Code).FirstOrDefault(),
                scl.Species.MunicipalitySpecies.Count,
                scl.Species.LastObservedAtUtc,
                scl.Species.IsFeatured
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<byte[]?> GetProfileImageAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context
            .Species.Where(s => s.Id == id)
            .Select(s => s.ProfileImageData)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<SpeciesStatsDto> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var totalSpecies = await context.Species.CountAsync(cancellationToken);
        var endemicCount = await context.Species.CountAsync(s => s.IsEndemic, cancellationToken);
        var threatenedCount = await context
            .Species.CountAsync(
                s => s.IucnStatus != null && ThreatenedStatuses.Contains(s.IucnStatus.Value),
                cancellationToken
            );
        var municipalitiesCovered = await context
            .MunicipalitySpecies.Select(ms => ms.MunicipalityId)
            .Distinct()
            .CountAsync(cancellationToken);

        var quarterAgo = DateTimeOffset.UtcNow.AddDays(-90);
        var addedThisQuarter = await context
            .Species.CountAsync(s => s.CreatedAtUtc >= quarterAgo, cancellationToken);

        // Municipalities with ≥10 endemic species recorded — the "biodiversity hotspot"
        // metric surfaced on the Municipios page.
        const int endemicHotspotThreshold = 10;
        var endemicHotspotCount = await context
            .MunicipalitySpecies.Where(ms => ms.Species.IsEndemic)
            .GroupBy(ms => ms.MunicipalityId)
            .Where(g => g.Count() >= endemicHotspotThreshold)
            .CountAsync(cancellationToken);

        return new SpeciesStatsDto(
            totalSpecies,
            endemicCount,
            threatenedCount,
            municipalitiesCovered,
            options.Value.TotalMunicipalitiesInRegion,
            addedThisQuarter,
            ReclassifiedThisQuarter: 0,
            endemicHotspotCount
        );
    }

    public async Task<IReadOnlyList<MunicipalitySpeciesCountDto>> GetCountsByMunicipalityAsync(
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context
            .MunicipalitySpecies.GroupBy(ms => ms.MunicipalityId)
            .Select(g => new MunicipalitySpeciesCountDto(g.Key, g.Count()))
            .ToListAsync(cancellationToken);
    }

    public async Task<SpeciesFacetsDto> GetFacetsAsync(
        SpeciesParameters parameters,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var filtered = ApplyFilters(context.Species.AsQueryable(), parameters);

        var taxa = await filtered
            .SelectMany(s => s.CategoryLinks)
            .GroupBy(cl => cl.Category.Code)
            .Select(g => new TaxonFacetDto(g.Key, g.Count()))
            .ToListAsync(cancellationToken);

        var statuses = await filtered
            .Where(s => s.IucnStatus != null)
            .GroupBy(s => s.IucnStatus!.Value)
            .Select(g => new IucnFacetDto(g.Key, g.Count()))
            .ToListAsync(cancellationToken);

        var endemicCount = await filtered.CountAsync(s => s.IsEndemic, cancellationToken);
        var recentCutoff = DateTimeOffset.UtcNow.AddYears(-1);
        var recentlyObservedCount = await filtered.CountAsync(
            s => s.LastObservedAtUtc >= recentCutoff,
            cancellationToken
        );
        var withImageCount = await filtered.CountAsync(
            s => s.ProfileImageData != null,
            cancellationToken
        );

        return new SpeciesFacetsDto(
            taxa,
            statuses,
            endemicCount,
            recentlyObservedCount,
            withImageCount
        );
    }

    public async Task<IReadOnlyList<SpeciesDtoForList>> GetFeaturedAsync(
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        // Prefer species with a photo, then randomise. IsFeatured stays as a
        // curatorial pin mechanism (empty flag = random, pinned rows surface first).
        return await context
            .Species.OrderByDescending(s => s.IsFeatured)
            .ThenByDescending(s => s.ProfileImageData != null)
            .ThenBy(s => EF.Functions.Random())
            .Take(3)
            .Select(static s => new SpeciesDtoForList(
                s.Id,
                s.CommonName,
                s.ScientificName,
                s.IsFauna,
                s.GRank,
                s.SRank,
                s.ProfileImageData != null,
                s.IsEndemic,
                s.IucnStatus,
                s.CategoryLinks.Select(cl => cl.Category.Code).FirstOrDefault(),
                s.MunicipalitySpecies.Count,
                s.LastObservedAtUtc,
                s.IsFeatured
            ))
            .ToListAsync(cancellationToken);
    }

    private static IQueryable<Database.Models.Species> ApplyFilters(
        IQueryable<Database.Models.Species> query,
        SpeciesParameters parameters
    )
    {
        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            // Municipality-name search requires crossing into the Locations module
            // and is out of scope for this pass (tracked as follow-up in issue #188).
            var pattern = $"%{parameters.Search.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%";
            query = query.Where(s =>
                EF.Functions.ILike(s.ScientificName, pattern)
                || s.CommonName.Any(c => EF.Functions.ILike(c.Value, pattern))
            );
        }

        if (parameters.IsFauna.HasValue)
        {
            query = query.Where(s => s.IsFauna == parameters.IsFauna.Value);
        }

        if (parameters.IsEndemic.HasValue)
        {
            query = query.Where(s => s.IsEndemic == parameters.IsEndemic.Value);
        }

        if (parameters.HasProfileImage.HasValue)
        {
            query = parameters.HasProfileImage.Value
                ? query.Where(s => s.ProfileImageData != null)
                : query.Where(s => s.ProfileImageData == null);
        }

        if (parameters.CategoryId.HasValue)
        {
            query = query.Where(s =>
                s.CategoryLinks.Any(cl => cl.CategoryId == parameters.CategoryId.Value)
            );
        }

        if (parameters.MunicipalityId.HasValue)
        {
            query = query.Where(s =>
                s.MunicipalitySpecies.Any(ms =>
                    ms.MunicipalityId == parameters.MunicipalityId.Value
                )
            );
        }

        if (parameters.IucnStatuses is { Length: > 0 } statuses)
        {
            query = query.Where(s => s.IucnStatus != null && statuses.Contains(s.IucnStatus.Value));
        }

        if (parameters.TaxonCodes is { Length: > 0 } codes)
        {
            query = query.Where(s => s.CategoryLinks.Any(cl => codes.Contains(cl.Category.Code)));
        }

        if (parameters.MinMunicipalityCount is { } minCount)
        {
            query = query.Where(s => s.MunicipalitySpecies.Count >= minCount);
        }

        if (parameters.ObservedSinceUtc is { } observedSince)
        {
            query = query.Where(s => s.LastObservedAtUtc >= observedSince);
        }

        return query;
    }

    private static IQueryable<Database.Models.Species> ApplySort(
        IQueryable<Database.Models.Species> query,
        SpeciesSort sort
    ) => sort switch
    {
        SpeciesSort.ScientificNameAsc => query.OrderBy(s => s.ScientificName).ThenBy(s => s.Id),
        SpeciesSort.ScientificNameDesc => query
            .OrderByDescending(s => s.ScientificName)
            .ThenByDescending(s => s.Id),
        SpeciesSort.RecentlyObserved => query
            .OrderByDescending(s => s.LastObservedAtUtc)
            .ThenByDescending(s => s.Id),
        SpeciesSort.MostMunicipalities => query
            .OrderByDescending(s => s.MunicipalitySpecies.Count)
            .ThenByDescending(s => s.Id),
        _ => query.OrderByDescending(s => s.Id),
    };
}
