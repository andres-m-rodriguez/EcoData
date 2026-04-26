using System.Runtime.CompilerServices;
using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Parameters;
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

    public async IAsyncEnumerable<SurfaceWaterStationDto> GetStationsAsync(
        SurfaceWaterStationParameters parameters,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        // The full list is materialized + sorted on every request. The slice below
        // is what actually goes over the wire; DB cost stays the same per request.
        // For larger station counts, swap this for a snapshot table populated by a worker.
        var sorted = await ComputeSortedStationsAsync(sparklineSize: 12, cancellationToken);

        var startIndex = 0;
        if (parameters.Cursor.HasValue)
        {
            var idx = sorted.FindIndex(s => s.SensorId == parameters.Cursor.Value);
            startIndex = idx >= 0 ? idx + 1 : 0;
        }

        var pageSize = parameters.PageSize > 0 ? parameters.PageSize : 50;
        var end = Math.Min(sorted.Count, startIndex + pageSize);
        for (var i = startIndex; i < end; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return sorted[i] with { Rank = i + 1 };
        }
    }

    public async IAsyncEnumerable<SurfaceWaterStationMarkerDto> GetMarkersAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default
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
        {
            yield break;
        }

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

        var ordered = rows
            .Select(r => new
            {
                r.Id,
                r.Name,
                r.ExternalId,
                r.MunicipalityId,
                r.Latitude,
                r.Longitude,
                r.LatestFlow,
                r.LatestGage,
                LatestRecordedAt = MaxNullable(r.LatestFlowAt, r.LatestGageAt),
                Status = ResolveStatus(r.LatestFlow, MaxNullable(r.LatestFlowAt, r.LatestGageAt), dayAgo),
            })
            .OrderByDescending(r => r.LatestFlow ?? double.MinValue)
            .ThenByDescending(r => r.Id);

        foreach (var r in ordered)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return new SurfaceWaterStationMarkerDto(
                SensorId: r.Id,
                Name: r.Name,
                ExternalId: r.ExternalId,
                MunicipalityId: r.MunicipalityId,
                Latitude: r.Latitude,
                Longitude: r.Longitude,
                LatestStreamflowCfs: r.LatestFlow,
                LatestGageHeightFt: r.LatestGage,
                LatestRecordedAt: r.LatestRecordedAt,
                Status: r.Status
            );
        }
    }

    private async Task<List<SurfaceWaterStationDto>> ComputeSortedStationsAsync(
        int sparklineSize,
        CancellationToken cancellationToken
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
        {
            return [];
        }

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
                    .Take(sparklineSize)
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
