using EcoData.Wildlife.Contracts;

namespace FaunaFinder.Server.Reports;

public sealed record SpeciesReportLabels(
    string Code,
    string Title,
    string GeneratedOn,
    string Fauna,
    string Flora,
    string ElCode,
    string GlobalRank,
    string StateRank,
    string IucnStatus,
    string EndemicStatus,
    string Habitat,
    string LastObserved,
    string Categories,
    string Municipalities,
    string ConservationActions,
    string Practice,
    string Action,
    string Justification,
    string Locations,
    string Latitude,
    string Longitude,
    string RadiusMeters,
    string NoneRecorded,
    string ImageSource,
    string Page,
    string Of,
    IReadOnlyDictionary<EndemicStatus, string> EndemicStatuses
)
{
    public static SpeciesReportLabels For(string? code) =>
        string.Equals(code, "es", StringComparison.OrdinalIgnoreCase) ? Spanish : English;

    public static SpeciesReportLabels English { get; } =
        new(
            Code: "en",
            Title: "Species report",
            GeneratedOn: "Generated on",
            Fauna: "Fauna",
            Flora: "Flora",
            ElCode: "Element code",
            GlobalRank: "Global rank",
            StateRank: "State rank",
            IucnStatus: "IUCN status",
            EndemicStatus: "Endemic status",
            Habitat: "Habitat",
            LastObserved: "Last observed",
            Categories: "Categories",
            Municipalities: "Municipalities",
            ConservationActions: "Conservation actions",
            Practice: "NRCS practice",
            Action: "FWS action",
            Justification: "Justification",
            Locations: "Known locations",
            Latitude: "Latitude",
            Longitude: "Longitude",
            RadiusMeters: "Radius (m)",
            NoneRecorded: "None recorded",
            ImageSource: "Image source",
            Page: "Page",
            Of: "of",
            EndemicStatuses: new Dictionary<EndemicStatus, string>
            {
                [EcoData.Wildlife.Contracts.EndemicStatus.Unknown] = "Unknown",
                [EcoData.Wildlife.Contracts.EndemicStatus.Endemic] = "Endemic",
                [EcoData.Wildlife.Contracts.EndemicStatus.Native] = "Native",
                [EcoData.Wildlife.Contracts.EndemicStatus.Introduced] = "Introduced",
            }
        );

    public static SpeciesReportLabels Spanish { get; } =
        new(
            Code: "es",
            Title: "Informe de especie",
            GeneratedOn: "Generado el",
            Fauna: "Fauna",
            Flora: "Flora",
            ElCode: "Código de elemento",
            GlobalRank: "Rango global",
            StateRank: "Rango estatal",
            IucnStatus: "Estado UICN",
            EndemicStatus: "Estado endémico",
            Habitat: "Hábitat",
            LastObserved: "Última observación",
            Categories: "Categorías",
            Municipalities: "Municipios",
            ConservationActions: "Acciones de conservación",
            Practice: "Práctica NRCS",
            Action: "Acción FWS",
            Justification: "Justificación",
            Locations: "Ubicaciones conocidas",
            Latitude: "Latitud",
            Longitude: "Longitud",
            RadiusMeters: "Radio (m)",
            NoneRecorded: "Sin registros",
            ImageSource: "Fuente de la imagen",
            Page: "Página",
            Of: "de",
            EndemicStatuses: new Dictionary<EndemicStatus, string>
            {
                [EcoData.Wildlife.Contracts.EndemicStatus.Unknown] = "Desconocido",
                [EcoData.Wildlife.Contracts.EndemicStatus.Endemic] = "Endémica",
                [EcoData.Wildlife.Contracts.EndemicStatus.Native] = "Nativa",
                [EcoData.Wildlife.Contracts.EndemicStatus.Introduced] = "Introducida",
            }
        );
}
