using EcoData.AquaTrack.Database;
using Microsoft.EntityFrameworkCore;

namespace EcoData.AquaTrack.Seeder;

public sealed class DatabaseSeederWorker(
    IServiceProvider serviceProvider,
    IHostApplicationLifetime lifetime,
    ILogger<DatabaseSeederWorker> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();

            await MigrateAndSeedAsync(scope.ServiceProvider, stoppingToken);

            logger.LogInformation("Database migrations and seeding completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
        finally
        {
            lifetime.StopApplication();
        }
    }

    private async Task MigrateAndSeedAsync(
        IServiceProvider services,
        CancellationToken stoppingToken)
    {
        var context = services.GetRequiredService<AquaTrackDbContext>();

        logger.LogInformation("Applying AquaTrack database migrations...");
        await context.Database.MigrateAsync(stoppingToken);
        logger.LogInformation("AquaTrack database migrations applied.");

        // TODO: Add seeding logic here if needed
    }
}
