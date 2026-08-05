using EcoData.Sensors.Contracts.Dtos;

namespace EcoData.Sensors.DataAccess.Interfaces;

public interface ISurfaceWaterRepository
{
    Task<SurfaceWaterSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);

    Task<List<SurfaceWaterStationDto>> GetSortedStationsAsync(
        CancellationToken cancellationToken = default
    );

    Task<List<SurfaceWaterStationMarkerDto>> GetMarkersAsync(
        CancellationToken cancellationToken = default
    );
}
