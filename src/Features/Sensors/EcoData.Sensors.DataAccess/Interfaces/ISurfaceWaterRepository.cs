using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Parameters;

namespace EcoData.Sensors.DataAccess.Interfaces;

public interface ISurfaceWaterRepository
{
    Task<SurfaceWaterSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);

    Task<List<SurfaceWaterStationDto>> GetStationsAsync(
        SurfaceWaterStationParameters parameters,
        CancellationToken cancellationToken = default
    );

    Task<List<SurfaceWaterStationMarkerDto>> GetMarkersAsync(
        CancellationToken cancellationToken = default
    );
}
