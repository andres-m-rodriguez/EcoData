using EcoData.AquaTrack.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace EcoData.AquaTrack.Database;

public sealed class AquaTrackDbContext(DbContextOptions<AquaTrackDbContext> options) : DbContext(options)
{
    public DbSet<DataSource> DataSources => Set<DataSource>();
    public DbSet<Sensor> Sensors => Set<Sensor>();
    public DbSet<Reading> Readings => Set<Reading>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<IngestionLog> IngestionLogs => Set<IngestionLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new DataSource.EntityConfiguration());
        modelBuilder.ApplyConfiguration(new Sensor.EntityConfiguration());
        modelBuilder.ApplyConfiguration(new Reading.EntityConfiguration());
        modelBuilder.ApplyConfiguration(new Alert.EntityConfiguration());
        modelBuilder.ApplyConfiguration(new IngestionLog.EntityConfiguration());
    }
}
