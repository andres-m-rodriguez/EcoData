using EcoData.Common.Pagination;

namespace EcoData.Sensors.Contracts.Parameters;

public sealed record SensorParameters(
    int PageSize = 20,
    Guid? Cursor = null,
    string? Search = null,
    bool? IsActive = null,
    Guid? DataSourceId = null,
    Guid? OrganizationId = null,
    Guid? MunicipalityId = null
) : CursorParameters(PageSize, Cursor);
