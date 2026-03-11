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
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<SensorHealthConfig> SensorHealthConfigs => Set<SensorHealthConfig>();
    public DbSet<SensorHealthStatus> SensorHealthStatuses => Set<SensorHealthStatus>();
    public DbSet<SensorHealthAlert> SensorHealthAlerts => Set<SensorHealthAlert>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<SensorType> SensorTypes => Set<SensorType>();
    public DbSet<Parameter> Parameters => Set<Parameter>();
    public DbSet<OrganizationMember> OrganizationMembers => Set<OrganizationMember>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new DataSource.EntityConfiguration());
        modelBuilder.ApplyConfiguration(new Sensor.EntityConfiguration());
        modelBuilder.ApplyConfiguration(new Reading.EntityConfiguration());
        modelBuilder.ApplyConfiguration(new Alert.EntityConfiguration());
        modelBuilder.ApplyConfiguration(new IngestionLog.EntityConfiguration());
        modelBuilder.ApplyConfiguration(new Organization.EntityConfiguration());
        modelBuilder.ApplyConfiguration(new SensorHealthConfig.EntityConfiguration());
        modelBuilder.ApplyConfiguration(new SensorHealthStatus.EntityConfiguration());
        modelBuilder.ApplyConfiguration(new SensorHealthAlert.EntityConfiguration());
        modelBuilder.ApplyConfiguration(new ApiKey.EntityConfiguration());
        modelBuilder.ApplyConfiguration(new SensorType.EntityConfiguration());
        modelBuilder.ApplyConfiguration(new Parameter.EntityConfiguration());
        modelBuilder.ApplyConfiguration(new OrganizationMember.EntityConfiguration());
    }
}
