using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Parameters;

namespace EcoData.Sensors.DataAccess.Interfaces;

public interface ISurfaceWaterRepository
{
    Task<SurfaceWaterSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);

    IAsyncEnumerable<SurfaceWaterStationDto> GetStationsAsync(
        SurfaceWaterStationParameters parameters,
        CancellationToken cancellationToken = default
    );

    IAsyncEnumerable<SurfaceWaterStationMarkerDto> GetMarkersAsync(
        CancellationToken cancellationToken = default
    );
}
