namespace EcoData.Sensors.DataAccess.Interfaces;

public interface ISurfaceWaterSnapshotRefresher
{
    Task RefreshAsync(CancellationToken cancellationToken = default);
}
