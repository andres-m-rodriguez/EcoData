using System.Text.Json.Serialization;

namespace EcoData.AquaTrack.Ingestion.Models;

public sealed record UsgsResponse(
    [property: JsonPropertyName("value")] UsgsValue Value
);

public sealed record UsgsValue(
    [property: JsonPropertyName("timeSeries")] IReadOnlyList<UsgsTimeSeries> TimeSeries
);

public sealed record UsgsTimeSeries(
    [property: JsonPropertyName("sourceInfo")] UsgsSourceInfo SourceInfo,
    [property: JsonPropertyName("variable")] UsgsVariable Variable,
    [property: JsonPropertyName("values")] IReadOnlyList<UsgsValues> Values
);

public sealed record UsgsSourceInfo(
    [property: JsonPropertyName("siteName")] string SiteName,
    [property: JsonPropertyName("siteCode")] IReadOnlyList<UsgsSiteCode> SiteCode,
    [property: JsonPropertyName("geoLocation")] UsgsGeoLocation GeoLocation
);

public sealed record UsgsSiteCode(
    [property: JsonPropertyName("value")] string Value,
    [property: JsonPropertyName("agencyCode")] string? AgencyCode
);

public sealed record UsgsGeoLocation(
    [property: JsonPropertyName("geogLocation")] UsgsGeogLocation GeogLocation
);

public sealed record UsgsGeogLocation(
    [property: JsonPropertyName("latitude")] decimal Latitude,
    [property: JsonPropertyName("longitude")] decimal Longitude
);

public sealed record UsgsVariable(
    [property: JsonPropertyName("variableCode")] IReadOnlyList<UsgsVariableCode> VariableCode,
    [property: JsonPropertyName("variableName")] string? VariableName,
    [property: JsonPropertyName("unit")] UsgsUnit Unit
);

public sealed record UsgsVariableCode(
    [property: JsonPropertyName("value")] string Value
);

public sealed record UsgsUnit(
    [property: JsonPropertyName("unitCode")] string UnitCode
);

public sealed record UsgsValues(
    [property: JsonPropertyName("value")] IReadOnlyList<UsgsReading> Value
);

public sealed record UsgsReading(
    [property: JsonPropertyName("value")] string Value,
    [property: JsonPropertyName("dateTime")] DateTimeOffset DateTime
);
