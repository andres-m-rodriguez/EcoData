namespace EcoData.Wildlife.Contracts;

// Coverage denominator: Puerto Rico's 78 municipios plus the 3 U.S. Virgin Islands.
public static class MunicipalityCoverage
{
    public const int PuertoRicoMunicipios = 78;
    public const int UsVirginIslands = 3;
    public const int Total = PuertoRicoMunicipios + UsVirginIslands;
}
