using EcoData.Sensors.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace EcoData.Sensors.Database;

public sealed class SensorsDbContext(DbContextOptions<SensorsDbContext> options) : DbContext(options)
{
    public DbSet<Sensor> Sensors => Set<Sensor>();
    public DbSet<SensorType> SensorTypes => Set<SensorType>();
    public DbSet<Phenomenon> Phenomena => Set<Phenomenon>();
    public DbSet<Parameter> Parameters => Set<Parameter>();
    public DbSet<Reading> Readings => Set<Reading>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<SensorHealthConfig> SensorHealthConfigs => Set<SensorHealthConfig>();
    public DbSet<SensorHealthStatus> SensorHealthStatuses => Set<SensorHealthStatus>();
    public DbSet<SensorHealthAlert> SensorHealthAlerts => Set<SensorHealthAlert>();
    public DbSet<IngestionLog> IngestionLogs => Set<IngestionLog>();
    public DbSet<UserSensorSubscription> UserSensorSubscriptions => Set<UserSensorSubscription>();
    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();
    public DbSet<SurfaceWaterStationSnapshot> SurfaceWaterStationSnapshots => Set<SurfaceWaterStationSnapshot>();
    public DbSet<SurfaceWaterSummarySnapshot> SurfaceWaterSummarySnapshots => Set<SurfaceWaterSummarySnapshot>();
    public DbSet<ReadingStatsSnapshot> ReadingStatsSnapshots => Set<ReadingStatsSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("postgis");
        modelBuilder.ApplyConfiguration(new Sensor.EntityConfiguration());
        modelBuilder.ApplyConfiguration(new SensorType.EntityConfiguration());
        modelBuilder.ApplyConfiguration(new Phenomenon.EntityConfiguration());
        modelBuilder.ApplyConfiguration(new Parameter.EntityConfiguration());
        modelBuilder.ApplyConfiguration(new Reading.EntityConfiguration());
        modelBuilder.ApplyConfiguration(new Alert.EntityConfiguration());
        modelBuilder.ApplyConfiguration(new SensorHealthConfig.EntityConfiguration());
        modelBuilder.ApplyConfiguration(new SensorHealthStatus.EntityConfiguration());
        modelBuilder.ApplyConfiguration(new SensorHealthAlert.EntityConfiguration());
        modelBuilder.ApplyConfiguration(new IngestionLog.EntityConfiguration());
        modelBuilder.ApplyConfiguration(new UserSensorSubscription.EntityConfiguration());
        modelBuilder.ApplyConfiguration(new UserNotification.EntityConfiguration());
        modelBuilder.ApplyConfiguration(new SurfaceWaterStationSnapshot.EntityConfiguration());
        modelBuilder.ApplyConfiguration(new SurfaceWaterSummarySnapshot.EntityConfiguration());
        modelBuilder.ApplyConfiguration(new ReadingStatsSnapshot.EntityConfiguration());
    }
}
