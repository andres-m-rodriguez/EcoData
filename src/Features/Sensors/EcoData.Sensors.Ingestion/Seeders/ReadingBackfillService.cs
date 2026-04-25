using EcoData.Sensors.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EcoData.Sensors.Ingestion.Seeders;

// Resolves phenomenon_id, parameter_id, and canonical_value on any existing
// readings that were ingested before parameter mappings existed. Runs once on
// startup after PhenomenonSeeder. Idempotent: subsequent runs find no
// unresolved readings and exit immediately.
public sealed class ReadingBackfillService(
    IDbContextFactory<SensorsDbContext> contextFactory,
    ILogger<ReadingBackfillService> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var unresolvedCount = await context
            .Readings.AsNoTracking()
            .Where(r => r.PhenomenonId == null)
            .LongCountAsync(cancellationToken);

        if (unresolvedCount == 0)
        {
            logger.LogDebug("No unresolved readings to backfill");
            return;
        }

        logger.LogInformation(
            "Backfilling canonical values on {Count} unresolved reading(s)",
            unresolvedCount
        );

        var rowsUpdated = await context.Database.ExecuteSqlRawAsync(
            """
            UPDATE readings r
            SET phenomenon_id   = p.phenomenon_id,
                parameter_id    = p.id,
                canonical_value = r.value * p.unit_factor + p.unit_offset
            FROM sensors s, parameters p
            WHERE r.sensor_id = s.id
              AND p.source_id = s.source_id
              AND p.code = r.parameter
              AND r.phenomenon_id IS NULL
            """,
            cancellationToken
        );

        var stillUnresolved = unresolvedCount - rowsUpdated;
        if (stillUnresolved > 0)
        {
            logger.LogWarning(
                "Backfill resolved {Resolved} reading(s); {Remaining} remain unresolved (no parameter mapping for their source/code)",
                rowsUpdated,
                stillUnresolved
            );
        }
        else
        {
            logger.LogInformation("Backfill resolved {Resolved} reading(s)", rowsUpdated);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
