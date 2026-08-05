using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Parameters;
using EcoData.Sensors.DataAccess.Interfaces;
using EcoData.Sensors.Database;
using Microsoft.EntityFrameworkCore;

namespace EcoData.Sensors.DataAccess.Repositories;

/// <summary>
/// Reads the pre-computed snapshots maintained by
/// <see cref="Services.SurfaceWaterSnapshotRefresher"/>. Status is derived at
/// read time so stations age into Offline between refreshes without a write.
/// </summary>
public sealed class SurfaceWaterRepository(IDbContextFactory<SensorsDbContext> contextFactory)
    : ISurfaceWaterRepository
{
    public async Task<SurfaceWaterSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var summary = await context.SurfaceWaterSummarySnapshots
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (summary is null)
        {
            return new SurfaceWaterSummaryDto(
                StationsReporting: 0,
                TotalStations: 0,
                Readings7d: 0,
                MedianStreamflowCfs: null,
                MeanGageHeightFt: null,
                MeanRainfallInches7d: null,
                ActiveAlerts: 0
            );
        }

        return new SurfaceWaterSummaryDto(
            StationsReporting: summary.StationsReporting,
            TotalStations: summary.TotalStations,
            Readings7d: summary.Readings7d,
            MedianStreamflowCfs: summary.MedianStreamflowCfs,
            MeanGageHeightFt: summary.MeanGageHeightFt,
            MeanRainfallInches7d: summary.MeanRainfallInches7d,
            ActiveAlerts: 0
        );
    }

    public async Task<List<SurfaceWaterStationDto>> GetStationsAsync(
        SurfaceWaterStationParameters parameters,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var afterRank = 0;
        if (parameters.Cursor.HasValue)
        {
            // An unknown cursor resolves to rank 0, i.e. the first page.
            afterRank = await context.SurfaceWaterStationSnapshots
                .Where(s => s.SensorId == parameters.Cursor.Value)
                .Select(s => s.Rank)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var pageSize = parameters.PageSize > 0 ? parameters.PageSize : 50;

        var rows = await context.SurfaceWaterStationSnapshots
            .AsNoTracking()
            .Where(s => s.Rank > afterRank)
            .OrderBy(s => s.Rank)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dayAgo = DateTimeOffset.UtcNow.AddDays(-1);

        return rows
            .Select(s => new SurfaceWaterStationDto(
                Rank: s.Rank,
                SensorId: s.SensorId,
                Name: s.Name,
                ExternalId: s.ExternalId,
                MunicipalityId: s.MunicipalityId,
                Latitude: s.Latitude,
                Longitude: s.Longitude,
                LatestStreamflowCfs: s.LatestStreamflowCfs,
                LatestGageHeightFt: s.LatestGageHeightFt,
                LatestRecordedAt: s.LatestRecordedAt,
                Status: ResolveStatus(s.LatestStreamflowCfs, s.LatestRecordedAt, dayAgo),
                SparklineFlow: s.SparklineFlow
            ))
            .ToList();
    }

    public async Task<List<SurfaceWaterStationMarkerDto>> GetMarkersAsync(
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var rows = await context.SurfaceWaterStationSnapshots
            .AsNoTracking()
            .OrderBy(s => s.Rank)
            .ToListAsync(cancellationToken);

        var dayAgo = DateTimeOffset.UtcNow.AddDays(-1);

        return rows
            .Select(s => new SurfaceWaterStationMarkerDto(
                SensorId: s.SensorId,
                Name: s.Name,
                ExternalId: s.ExternalId,
                MunicipalityId: s.MunicipalityId,
                Latitude: s.Latitude,
                Longitude: s.Longitude,
                LatestStreamflowCfs: s.LatestStreamflowCfs,
                LatestGageHeightFt: s.LatestGageHeightFt,
                LatestRecordedAt: s.LatestRecordedAt,
                Status: ResolveStatus(s.LatestStreamflowCfs, s.LatestRecordedAt, dayAgo)
            ))
            .ToList();
    }

    private static string ResolveStatus(double? latestFlow, DateTimeOffset? latestRecordedAt, DateTimeOffset dayAgo)
    {
        if (latestRecordedAt is null || latestRecordedAt < dayAgo) return "Offline";
        if (latestFlow is null) return "Normal";
        return latestFlow switch
        {
            >= 2000 => "High",
            >= 500 => "Elevated",
            < 20 => "Low",
            _ => "Normal",
        };
    }
}
