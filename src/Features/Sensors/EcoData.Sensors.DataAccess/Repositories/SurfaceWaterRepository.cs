using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.DataAccess.Interfaces;
using EcoData.Sensors.Database;
using Microsoft.EntityFrameworkCore;

namespace EcoData.Sensors.DataAccess.Repositories;

public sealed class SurfaceWaterRepository(IDbContextFactory<SensorsDbContext> contextFactory)
    : ISurfaceWaterRepository
{
    private const string StreamflowCode = "00060";
    private const string GageHeightCode = "00065";
    private const string PrecipitationCode = "00045";
    private const int SparklineSize = 12;

    private static readonly string[] SurfaceWaterCodes =
        [StreamflowCode, GageHeightCode, PrecipitationCode];

    public async Task<SurfaceWaterSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var dayAgo = now.AddDays(-1);
        var weekAgo = now.AddDays(-7);

        var totalStations = await context.Readings
            .Where(r => SurfaceWaterCodes.Contains(r.Parameter))
            .Select(r => r.SensorId)
            .Distinct()
            .CountAsync(cancellationToken);

        var stationsReporting = await context.Readings
            .Where(r => SurfaceWaterCodes.Contains(r.Parameter) && r.RecordedAt >= dayAgo)
            .Select(r => r.SensorId)
            .Distinct()
            .CountAsync(cancellationToken);

        var readings7d = await context.Readings
            .Where(r => SurfaceWaterCodes.Contains(r.Parameter) && r.RecordedAt >= weekAgo)
            .LongCountAsync(cancellationToken);

        var latestPerSensor = await context.Readings
            .Where(r => r.Parameter == StreamflowCode && r.RecordedAt >= dayAgo)
            .GroupBy(r => r.SensorId)
            .Select(g => g.OrderByDescending(r => r.RecordedAt).Select(r => r.Value).First())
            .ToListAsync(cancellationToken);

        var medianStreamflow = Median(latestPerSensor);

        var latestGagePerSensor = await context.Readings
            .Where(r => r.Parameter == GageHeightCode && r.RecordedAt >= dayAgo)
            .GroupBy(r => r.SensorId)
            .Select(g => g.OrderByDescending(r => r.RecordedAt).Select(r => r.Value).First())
            .ToListAsync(cancellationToken);

        var meanGage = latestGagePerSensor.Count > 0 ? latestGagePerSensor.Average() : (double?)null;

        var rainfallTotalsPerSensor = await context.Readings
            .Where(r => r.Parameter == PrecipitationCode && r.RecordedAt >= weekAgo)
            .GroupBy(r => r.SensorId)
            .Select(g => g.Sum(r => r.Value))
            .ToListAsync(cancellationToken);

        var meanRainfall = rainfallTotalsPerSensor.Count > 0 ? rainfallTotalsPerSensor.Average() : (double?)null;

        return new SurfaceWaterSummaryDto(
            StationsReporting: stationsReporting,
            TotalStations: totalStations,
            Readings7d: readings7d,
            MedianStreamflowCfs: medianStreamflow,
            MeanGageHeightFt: meanGage,
            MeanRainfallInches7d: meanRainfall,
            ActiveAlerts: 0
        );
    }

    public async Task<List<SurfaceWaterStationMarkerDto>> GetMarkersAsync(
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var dayAgo = DateTimeOffset.UtcNow.AddDays(-1);

        var sensorIds = await context.Readings
            .Where(r => r.Parameter == StreamflowCode || r.Parameter == GageHeightCode)
            .Select(r => r.SensorId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (sensorIds.Count == 0)
            return [];

        var rows = await context.Sensors
            .Where(s => sensorIds.Contains(s.Id))
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.ExternalId,
                s.MunicipalityId,
                s.Latitude,
                s.Longitude,
                LatestFlow = context.Readings
                    .Where(r => r.SensorId == s.Id && r.Parameter == StreamflowCode)
                    .OrderByDescending(r => r.RecordedAt)
                    .Select(r => (double?)r.Value)
                    .FirstOrDefault(),
                LatestFlowAt = context.Readings
                    .Where(r => r.SensorId == s.Id && r.Parameter == StreamflowCode)
                    .OrderByDescending(r => r.RecordedAt)
                    .Select(r => (DateTimeOffset?)r.RecordedAt)
                    .FirstOrDefault(),
                LatestGage = context.Readings
                    .Where(r => r.SensorId == s.Id && r.Parameter == GageHeightCode)
                    .OrderByDescending(r => r.RecordedAt)
                    .Select(r => (double?)r.Value)
                    .FirstOrDefault(),
                LatestGageAt = context.Readings
                    .Where(r => r.SensorId == s.Id && r.Parameter == GageHeightCode)
                    .OrderByDescending(r => r.RecordedAt)
                    .Select(r => (DateTimeOffset?)r.RecordedAt)
                    .FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(r =>
            {
                var latestRecorded = MaxNullable(r.LatestFlowAt, r.LatestGageAt);
                return new SurfaceWaterStationMarkerDto(
                    SensorId: r.Id,
                    Name: r.Name,
                    ExternalId: r.ExternalId,
                    MunicipalityId: r.MunicipalityId,
                    Latitude: r.Latitude,
                    Longitude: r.Longitude,
                    LatestStreamflowCfs: r.LatestFlow,
                    LatestGageHeightFt: r.LatestGage,
                    LatestRecordedAt: latestRecorded,
                    Status: ResolveStatus(r.LatestFlow, latestRecorded, dayAgo)
                );
            })
            .OrderByDescending(r => r.LatestStreamflowCfs ?? double.MinValue)
            .ThenByDescending(r => r.SensorId)
            .ToList();
    }

    public async Task<List<SurfaceWaterStationDto>> GetSortedStationsAsync(
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var dayAgo = DateTimeOffset.UtcNow.AddDays(-1);

        var sensorIds = await context.Readings
            .Where(r => r.Parameter == StreamflowCode || r.Parameter == GageHeightCode)
            .Select(r => r.SensorId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (sensorIds.Count == 0)
            return [];

        var stations = await context.Sensors
            .Where(s => sensorIds.Contains(s.Id))
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.ExternalId,
                s.MunicipalityId,
                s.Latitude,
                s.Longitude,
                LatestFlow = context.Readings
                    .Where(r => r.SensorId == s.Id && r.Parameter == StreamflowCode)
                    .OrderByDescending(r => r.RecordedAt)
                    .Select(r => (double?)r.Value)
                    .FirstOrDefault(),
                LatestFlowAt = context.Readings
                    .Where(r => r.SensorId == s.Id && r.Parameter == StreamflowCode)
                    .OrderByDescending(r => r.RecordedAt)
                    .Select(r => (DateTimeOffset?)r.RecordedAt)
                    .FirstOrDefault(),
                LatestGage = context.Readings
                    .Where(r => r.SensorId == s.Id && r.Parameter == GageHeightCode)
                    .OrderByDescending(r => r.RecordedAt)
                    .Select(r => (double?)r.Value)
                    .FirstOrDefault(),
                LatestGageAt = context.Readings
                    .Where(r => r.SensorId == s.Id && r.Parameter == GageHeightCode)
                    .OrderByDescending(r => r.RecordedAt)
                    .Select(r => (DateTimeOffset?)r.RecordedAt)
                    .FirstOrDefault(),
                Sparkline = context.Readings
                    .Where(r => r.SensorId == s.Id && r.Parameter == StreamflowCode)
                    .OrderByDescending(r => r.RecordedAt)
                    .Take(SparklineSize)
                    .Select(r => r.Value)
                    .ToList(),
            })
            .ToListAsync(cancellationToken);

        return stations
            .Select(s =>
            {
                var latestRecorded = MaxNullable(s.LatestFlowAt, s.LatestGageAt);
                var status = ResolveStatus(s.LatestFlow, latestRecorded, dayAgo);
                var sparkline = s.Sparkline.AsEnumerable().Reverse().ToList();

                return new SurfaceWaterStationDto(
                    Rank: 0,
                    SensorId: s.Id,
                    Name: s.Name,
                    ExternalId: s.ExternalId,
                    MunicipalityId: s.MunicipalityId,
                    Latitude: s.Latitude,
                    Longitude: s.Longitude,
                    LatestStreamflowCfs: s.LatestFlow,
                    LatestGageHeightFt: s.LatestGage,
                    LatestRecordedAt: latestRecorded,
                    Status: status,
                    SparklineFlow: sparkline
                );
            })
            .OrderByDescending(s => s.LatestStreamflowCfs ?? double.MinValue)
            .ThenByDescending(s => s.SensorId)
            .Select((s, i) => s with { Rank = i + 1 })
            .ToList();
    }

    private static double? Median(List<double> values)
    {
        if (values.Count == 0) return null;
        values.Sort();
        var mid = values.Count / 2;
        return values.Count % 2 == 0
            ? (values[mid - 1] + values[mid]) / 2.0
            : values[mid];
    }

    private static DateTimeOffset? MaxNullable(DateTimeOffset? a, DateTimeOffset? b)
    {
        if (a is null) return b;
        if (b is null) return a;
        return a > b ? a : b;
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
