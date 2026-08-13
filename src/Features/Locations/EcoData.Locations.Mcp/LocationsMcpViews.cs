namespace EcoData.Locations.Mcp;

// What the location tools hand back, deliberately narrower than the DTOs the
// web app reads. The boundary polygon in particular never crosses this line:
// it is thousands of coordinates, useful to a map and useless in a context
// window.

public sealed record MunicipalitySummary(
    Guid Id,
    string Name,
    double CentroidLatitude,
    double CentroidLongitude
);

public sealed record MunicipalityDetail(
    Guid Id,
    string Name,
    string State,
    string StateCode,
    string CountyFipsCode,
    double CentroidLatitude,
    double CentroidLongitude
);
