using EcoData.Sensors.DataAccess.Interfaces;
using EcoData.Sensors.Database;
using EcoData.Sensors.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace EcoData.Sensors.DataAccess.Services;

/// <summary>
/// Recomputes the surface-water dashboard snapshots from the readings table and
/// rewrites them in one transaction. Runs after each ingestion cycle, so
/// dashboard reads never pay for these aggregations.
/// </summary>
public sealed class SurfaceWaterSnapshotRefresher(IDbContextFactory<SensorsDbContext> contextFactory)
    : ISurfaceWaterSnapshotRefresher
{
    private const string StreamflowCode = "00060";
    private const string GageHeightCode = "00065";
    private const string PrecipitationCode = "00045";
    private const int SparklineSize = 12;

    private static readonly string[] SurfaceWaterCodes =
        [StreamflowCode, GageHeightCode, PrecipitationCode];

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var dayAgo = now.AddDays(-1);
        var weekAgo = now.AddDays(-7);

        var stations = await ComputeStationSnapshotsAsync(context, now, cancellationToken);
        var summary = await ComputeSummarySnapshotAsync(context, now, dayAgo, weekAgo, cancellationToken);
        var totalReadings = await context.Readings.LongCountAsync(cancellationToken);

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        await context.SurfaceWaterStationSnapshots.ExecuteDeleteAsync(cancellationToken);
        await context.SurfaceWaterSummarySnapshots.ExecuteDeleteAsync(cancellationToken);
        await context.ReadingStatsSnapshots.ExecuteDeleteAsync(cancellationToken);

        context.SurfaceWaterStationSnapshots.AddRange(stations);
        context.SurfaceWaterSummarySnapshots.Add(summary);
        context.ReadingStatsSnapshots.Add(new ReadingStatsSnapshot
        {
            Id = 1,
            TotalReadings = totalReadings,
            ComputedAt = now,
        });

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task<List<SurfaceWaterStationSnapshot>> ComputeStationSnapshotsAsync(
        SensorsDbContext context,
        DateTimeOffset now,
        CancellationToken cancellationToken
    )
    {
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
                    .Take(SparklineSize)
                    .Select(r => r.Value)
                    .ToList(),
            })
            .ToListAsync(cancellationToken);

        return stations
            .OrderByDescending(s => s.LatestFlow ?? double.MinValue)
            .ThenByDescending(s => s.Id)
            .Select((s, i) => new SurfaceWaterStationSnapshot
            {
                SensorId = s.Id,
                Rank = i + 1,
                Name = s.Name,
                ExternalId = s.ExternalId,
                MunicipalityId = s.MunicipalityId,
                Latitude = s.Latitude,
                Longitude = s.Longitude,
                LatestStreamflowCfs = s.LatestFlow,
                LatestGageHeightFt = s.LatestGage,
                LatestRecordedAt = MaxNullable(s.LatestFlowAt, s.LatestGageAt),
                SparklineFlow = s.Sparkline.AsEnumerable().Reverse().ToList(),
                ComputedAt = now,
            })
            .ToList();
    }

    private static async Task<SurfaceWaterSummarySnapshot> ComputeSummarySnapshotAsync(
        SensorsDbContext context,
        DateTimeOffset now,
        DateTimeOffset dayAgo,
        DateTimeOffset weekAgo,
        CancellationToken cancellationToken
    )
    {
        // Counts sensors across all three parameter codes, so precipitation-only
        // stations are included even though they never appear in the station list.
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

        var latestFlowPerSensor = await context.Readings
            .Where(r => r.Parameter == StreamflowCode && r.RecordedAt >= dayAgo)
            .GroupBy(r => r.SensorId)
            .Select(g => g.OrderByDescending(r => r.RecordedAt).Select(r => r.Value).First())
            .ToListAsync(cancellationToken);

        var latestGagePerSensor = await context.Readings
            .Where(r => r.Parameter == GageHeightCode && r.RecordedAt >= dayAgo)
            .GroupBy(r => r.SensorId)
            .Select(g => g.OrderByDescending(r => r.RecordedAt).Select(r => r.Value).First())
            .ToListAsync(cancellationToken);

        var rainfallTotalsPerSensor = await context.Readings
            .Where(r => r.Parameter == PrecipitationCode && r.RecordedAt >= weekAgo)
            .GroupBy(r => r.SensorId)
            .Select(g => g.Sum(r => r.Value))
            .ToListAsync(cancellationToken);

        return new SurfaceWaterSummarySnapshot
        {
            Id = 1,
            TotalStations = totalStations,
            StationsReporting = stationsReporting,
            Readings7d = readings7d,
            MedianStreamflowCfs = Median(latestFlowPerSensor),
            MeanGageHeightFt = latestGagePerSensor.Count > 0 ? latestGagePerSensor.Average() : null,
            MeanRainfallInches7d = rainfallTotalsPerSensor.Count > 0 ? rainfallTotalsPerSensor.Average() : null,
            ComputedAt = now,
        };
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
}
