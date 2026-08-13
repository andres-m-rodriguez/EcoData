using System.Runtime.CompilerServices;
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
    /// <summary>
    /// What rows seeded before the content type was recorded are served as, and
    /// what the image endpoint returned unconditionally before the move to blobs.
    /// </summary>
    private const string DefaultImageContentType = "image/jpeg";

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
                s.ProfileImageBlobName != null,
                s.CategoryLinks
                    .Select(cl => new SpeciesCategoryDtoForList(
                        cl.Category.Id,
                        cl.Category.Code,
                        cl.Category.Name
                    ))
                    .ToList(),
                s.MunicipalitySpecies.Select(ms => ms.MunicipalityId).ToList(),
                s.EndemicStatus,
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
        var speciesWithLocations = await context
            .Species.AsNoTracking()
            .Where(s => s.Locations.Any())
            .Select(s => new
            {
                s.Id,
                s.CommonName,
                s.ScientificName,
                s.IsFauna,
                Locations = s
                    .Locations.Select(l => new SpeciesLocationDto(
                        l.Id,
                        l.Latitude,
                        l.Longitude,
                        l.RadiusMeters,
                        l.Description
                    ))
                    .ToList(),
            })
            .ToListAsync(cancellationToken);

        var results = new List<SpeciesNearbyDto>();

        foreach (var species in speciesWithLocations)
        {
            foreach (var location in species.Locations)
            {
                const double earthRadiusMeters = 6371000;

                var dLat = (location.Latitude - latitude) * Math.PI / 180;
                var dLon = (location.Longitude - longitude) * Math.PI / 180;

                var a =
                    Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                    + Math.Cos(latitude * Math.PI / 180)
                        * Math.Cos(location.Latitude * Math.PI / 180)
                        * Math.Sin(dLon / 2)
                        * Math.Sin(dLon / 2);

                var distance =
                    earthRadiusMeters * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

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

        // One row per species: the occurrence closest to the search point.
        return results
            .GroupBy(r => r.Id)
            .Select(g => g.OrderBy(r => r.DistanceMeters).First())
            .OrderBy(r => r.DistanceMeters)
            .ToList();
    }

    public async Task<IReadOnlyList<SpeciesNearbyDto>> GetInPolygonAsync(
        IReadOnlyList<PolygonCoordinate> coordinates,
        CancellationToken cancellationToken = default
    )
    {
        if (coordinates.Count < 3)
            return [];

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var speciesWithLocations = await context
            .Species.AsNoTracking()
            .Where(s => s.Locations.Any())
            .Select(s => new
            {
                s.Id,
                s.CommonName,
                s.ScientificName,
                s.IsFauna,
                Locations = s
                    .Locations.Select(l => new SpeciesLocationDto(
                        l.Id,
                        l.Latitude,
                        l.Longitude,
                        l.RadiusMeters,
                        l.Description
                    ))
                    .ToList(),
            })
            .ToListAsync(cancellationToken);

        var centroidLat = coordinates.Average(c => c.Latitude);
        var centroidLng = coordinates.Average(c => c.Longitude);

        var results = new List<SpeciesNearbyDto>();

        foreach (var species in speciesWithLocations)
        {
            foreach (var location in species.Locations)
            {
                // Ray casting: count edge crossings of a horizontal ray from the point.
                var inside = false;
                for (int i = 0, j = coordinates.Count - 1; i < coordinates.Count; j = i++)
                {
                    var xi = coordinates[i].Longitude;
                    var yi = coordinates[i].Latitude;
                    var xj = coordinates[j].Longitude;
                    var yj = coordinates[j].Latitude;

                    if (
                        ((yi > location.Latitude) != (yj > location.Latitude))
                        && (
                            location.Longitude
                            < (xj - xi) * (location.Latitude - yi) / (yj - yi) + xi
                        )
                    )
                    {
                        inside = !inside;
                    }
                }

                if (!inside)
                    continue;

                const double earthRadiusMeters = 6371000;

                var dLat = (location.Latitude - centroidLat) * Math.PI / 180;
                var dLon = (location.Longitude - centroidLng) * Math.PI / 180;

                var a =
                    Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                    + Math.Cos(centroidLat * Math.PI / 180)
                        * Math.Cos(location.Latitude * Math.PI / 180)
                        * Math.Sin(dLon / 2)
                        * Math.Sin(dLon / 2);

                var distance =
                    earthRadiusMeters * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

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

        // One row per species: the occurrence closest to the polygon centroid.
        return results
            .GroupBy(r => r.Id)
            .Select(g => g.OrderBy(r => r.DistanceMeters).First())
            .OrderBy(r => r.DistanceMeters)
            .ToList();
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

    public async IAsyncEnumerable<SpeciesDtoForList> GetSpeciesAsync(
        SpeciesParameters parameters,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var query = context.Species.AsQueryable();

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            // Municipality-name search requires crossing into the Locations module
            // and is out of scope for this pass (tracked as follow-up in issue #188).
            var pattern =
                $"%{parameters.Search.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%";
            query = query.Where(s =>
                EF.Functions.ILike(s.ScientificName, pattern)
                || s.CommonName.Any(c => EF.Functions.ILike(c.Value, pattern))
            );
        }

        if (parameters.IsFauna.HasValue)
        {
            query = query.Where(s => s.IsFauna == parameters.IsFauna.Value);
        }

        if (parameters.EndemicStatuses is { Length: > 0 })
        {
            query = query.Where(s => parameters.EndemicStatuses.Contains(s.EndemicStatus));
        }

        if (parameters.HasProfileImage.HasValue)
        {
            query = parameters.HasProfileImage.Value
                ? query.Where(s => s.ProfileImageBlobName != null)
                : query.Where(s => s.ProfileImageBlobName == null);
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
                s.MunicipalitySpecies.Any(ms => ms.MunicipalityId == parameters.MunicipalityId.Value)
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

        if (parameters.NrcsPracticeCodes is { Length: > 0 } nrcsCodes)
        {
            query = query.Where(s =>
                s.FwsLinks.Any(l => nrcsCodes.Contains(l.NrcsPractice.Code))
            );
        }

        if (parameters.FwsActionCodes is { Length: > 0 } fwsCodes)
        {
            query = query.Where(s => s.FwsLinks.Any(l => fwsCodes.Contains(l.FwsAction.Code)));
        }

        if (parameters.Cursor.HasValue)
        {
            // Keyset pagination has to compare on the same key the rows are ordered
            // by. The cursor only carries an Id, so the row it points at supplies
            // the rest of its sort position; without that, "id < cursor" against an
            // ORDER BY on the name returns rows the caller has already seen.
            var cursor = await context
                .Species.Where(s => s.Id == parameters.Cursor.Value)
                .Select(s => new
                {
                    s.Id,
                    s.ScientificName,
                    s.LastObservedAtUtc,
                    MunicipalityCount = s.MunicipalitySpecies.Count,
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (cursor is not null)
            {
                // Each branch mirrors the matching ordering below, including its Id
                // tiebreaker; the two must be changed together.
                query = parameters.Sort switch
                {
                    SpeciesSort.ScientificNameAsc => query.Where(s =>
                        string.Compare(s.ScientificName, cursor.ScientificName) > 0
                        || (s.ScientificName == cursor.ScientificName && s.Id > cursor.Id)
                    ),
                    SpeciesSort.ScientificNameDesc => query.Where(s =>
                        string.Compare(s.ScientificName, cursor.ScientificName) < 0
                        || (s.ScientificName == cursor.ScientificName && s.Id < cursor.Id)
                    ),
                    // Postgres sorts NULLs first under DESC, so an unobserved cursor is
                    // still inside the leading null block and every dated row comes after it.
                    SpeciesSort.RecentlyObserved => cursor.LastObservedAtUtc is null
                        ? query.Where(s =>
                            s.LastObservedAtUtc != null
                            || (s.LastObservedAtUtc == null && s.Id < cursor.Id)
                        )
                        : query.Where(s =>
                            s.LastObservedAtUtc != null
                            && (
                                s.LastObservedAtUtc < cursor.LastObservedAtUtc
                                || (
                                    s.LastObservedAtUtc == cursor.LastObservedAtUtc
                                    && s.Id < cursor.Id
                                )
                            )
                        ),
                    SpeciesSort.MostMunicipalities => query.Where(s =>
                        s.MunicipalitySpecies.Count < cursor.MunicipalityCount
                        || (
                            s.MunicipalitySpecies.Count == cursor.MunicipalityCount
                            && s.Id < cursor.Id
                        )
                    ),
                    _ => query.Where(s => s.Id < cursor.Id),
                };
            }
        }

        query = parameters.Sort switch
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
                    s.ProfileImageBlobName != null,
                    s.EndemicStatus,
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

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var pattern =
                $"%{parameters.Search.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%";
            query = query.Where(s =>
                EF.Functions.ILike(s.ScientificName, pattern)
                || s.CommonName.Any(c => EF.Functions.ILike(c.Value, pattern))
            );
        }

        if (parameters.IsFauna.HasValue)
        {
            query = query.Where(s => s.IsFauna == parameters.IsFauna.Value);
        }

        if (parameters.EndemicStatuses is { Length: > 0 })
        {
            query = query.Where(s => parameters.EndemicStatuses.Contains(s.EndemicStatus));
        }

        if (parameters.HasProfileImage.HasValue)
        {
            query = parameters.HasProfileImage.Value
                ? query.Where(s => s.ProfileImageBlobName != null)
                : query.Where(s => s.ProfileImageBlobName == null);
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
                s.MunicipalitySpecies.Any(ms => ms.MunicipalityId == parameters.MunicipalityId.Value)
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

        if (parameters.NrcsPracticeCodes is { Length: > 0 } nrcsCodes)
        {
            query = query.Where(s =>
                s.FwsLinks.Any(l => nrcsCodes.Contains(l.NrcsPractice.Code))
            );
        }

        if (parameters.FwsActionCodes is { Length: > 0 } fwsCodes)
        {
            query = query.Where(s => s.FwsLinks.Any(l => fwsCodes.Contains(l.FwsAction.Code)));
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<SpeciesImageReference?> GetProfileImageReferenceAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        // A species without an image projects to null, so callers 404 on the row
        // alone rather than going to blob storage to find nothing there.
        return await context
            .Species.Where(s => s.Id == id && s.ProfileImageBlobName != null)
            .Select(s => new SpeciesImageReference(
                s.ProfileImageBlobName!,
                s.ProfileImageContentType ?? DefaultImageContentType
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<SpeciesStatsDto> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        IucnStatus[] threatenedStatuses = [IucnStatus.VU, IucnStatus.EN, IucnStatus.CR];

        var totalSpecies = await context.Species.CountAsync(cancellationToken);
        var endemicCount = await context.Species.CountAsync(
            s => s.EndemicStatus == EndemicStatus.Endemic,
            cancellationToken
        );
        var threatenedCount = await context
            .Species.CountAsync(
                s => s.IucnStatus != null && threatenedStatuses.Contains(s.IucnStatus.Value),
                cancellationToken
            );
        var municipalitiesCovered = await context
            .MunicipalitySpecies.Select(ms => ms.MunicipalityId)
            .Distinct()
            .CountAsync(cancellationToken);

        var quarterAgo = DateTimeOffset.UtcNow.AddDays(-90);
        var addedThisQuarter = await context.Species.CountAsync(
            s => s.CreatedAtUtc >= quarterAgo,
            cancellationToken
        );

        // Municipalities with ≥10 endemic species recorded — the "biodiversity hotspot"
        // metric surfaced on the Municipios page.
        const int endemicHotspotThreshold = 10;
        var endemicHotspotCount = await context
            .MunicipalitySpecies.Where(ms => ms.Species.EndemicStatus == EndemicStatus.Endemic)
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

        var filtered = context.Species.AsQueryable();

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var pattern =
                $"%{parameters.Search.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%";
            filtered = filtered.Where(s =>
                EF.Functions.ILike(s.ScientificName, pattern)
                || s.CommonName.Any(c => EF.Functions.ILike(c.Value, pattern))
            );
        }

        if (parameters.IsFauna.HasValue)
        {
            filtered = filtered.Where(s => s.IsFauna == parameters.IsFauna.Value);
        }

        if (parameters.EndemicStatuses is { Length: > 0 })
        {
            filtered = filtered.Where(s => parameters.EndemicStatuses.Contains(s.EndemicStatus));
        }

        if (parameters.HasProfileImage.HasValue)
        {
            filtered = parameters.HasProfileImage.Value
                ? filtered.Where(s => s.ProfileImageBlobName != null)
                : filtered.Where(s => s.ProfileImageBlobName == null);
        }

        if (parameters.CategoryId.HasValue)
        {
            filtered = filtered.Where(s =>
                s.CategoryLinks.Any(cl => cl.CategoryId == parameters.CategoryId.Value)
            );
        }

        if (parameters.MunicipalityId.HasValue)
        {
            filtered = filtered.Where(s =>
                s.MunicipalitySpecies.Any(ms => ms.MunicipalityId == parameters.MunicipalityId.Value)
            );
        }

        if (parameters.IucnStatuses is { Length: > 0 } statuses)
        {
            filtered = filtered.Where(s =>
                s.IucnStatus != null && statuses.Contains(s.IucnStatus.Value)
            );
        }

        if (parameters.TaxonCodes is { Length: > 0 } codes)
        {
            filtered = filtered.Where(s =>
                s.CategoryLinks.Any(cl => codes.Contains(cl.Category.Code))
            );
        }

        if (parameters.MinMunicipalityCount is { } minCount)
        {
            filtered = filtered.Where(s => s.MunicipalitySpecies.Count >= minCount);
        }

        if (parameters.ObservedSinceUtc is { } observedSince)
        {
            filtered = filtered.Where(s => s.LastObservedAtUtc >= observedSince);
        }

        if (parameters.NrcsPracticeCodes is { Length: > 0 } nrcsCodes)
        {
            filtered = filtered.Where(s =>
                s.FwsLinks.Any(l => nrcsCodes.Contains(l.NrcsPractice.Code))
            );
        }

        if (parameters.FwsActionCodes is { Length: > 0 } fwsCodes)
        {
            filtered = filtered.Where(s => s.FwsLinks.Any(l => fwsCodes.Contains(l.FwsAction.Code)));
        }

        var taxa = await filtered
            .SelectMany(s => s.CategoryLinks)
            .GroupBy(cl => cl.Category.Code)
            .Select(g => new TaxonFacetDto(g.Key, g.Count()))
            .ToListAsync(cancellationToken);

        var statusFacets = await filtered
            .Where(s => s.IucnStatus != null)
            .GroupBy(s => s.IucnStatus!.Value)
            .Select(g => new IucnFacetDto(g.Key, g.Count()))
            .ToListAsync(cancellationToken);

        var endemicCount = await filtered.CountAsync(
            s => s.EndemicStatus == EndemicStatus.Endemic,
            cancellationToken
        );
        var recentCutoff = DateTimeOffset.UtcNow.AddYears(-1);
        var recentlyObservedCount = await filtered.CountAsync(
            s => s.LastObservedAtUtc >= recentCutoff,
            cancellationToken
        );
        var withImageCount = await filtered.CountAsync(
            s => s.ProfileImageBlobName != null,
            cancellationToken
        );

        return new SpeciesFacetsDto(
            taxa,
            statusFacets,
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
            .ThenByDescending(s => s.ProfileImageBlobName != null)
            .ThenBy(s => EF.Functions.Random())
            .Take(3)
            .Select(static s => new SpeciesDtoForList(
                s.Id,
                s.CommonName,
                s.ScientificName,
                s.IsFauna,
                s.GRank,
                s.SRank,
                s.ProfileImageBlobName != null,
                s.EndemicStatus,
                s.IucnStatus,
                s.CategoryLinks.Select(cl => cl.Category.Code).FirstOrDefault(),
                s.MunicipalitySpecies.Count,
                s.LastObservedAtUtc,
                s.IsFeatured
            ))
            .ToListAsync(cancellationToken);
    }
}
