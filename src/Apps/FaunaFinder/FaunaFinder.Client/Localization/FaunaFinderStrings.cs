using EcoData.Common.i18n;

namespace FaunaFinder.Client.Localization;

/// <summary>
/// All user-visible UI strings for FaunaFinder, in both English and Spanish.
/// Feed into <see cref="Localizer"/> at DI registration.
///
/// <para>Key convention: <c>Feature_Section_Purpose</c> — e.g. <c>Species_Hero_Lede</c>,
/// <c>Muni_Stats_EndemicHotspots_Label</c>. Keep keys stable; translate values.</para>
///
/// <para>Brand names (FaunaFinder, Puerto Rico) stay as literals in markup.</para>
/// </summary>
public static class FaunaFinderStrings
{
    public static IReadOnlyList<ILanguage> Languages { get; } =
    [
        new Language("en", "English", IsDefault: true),
        new Language("es", "Español"),
    ];

    public static IReadOnlyList<ITranslation> Translations
    {
        get
        {
            var list = new List<ITranslation>(En.Count * 2);
            foreach (var (key, value) in En)
            {
                list.Add(new Translation("en", key, value));
            }
            foreach (var (key, value) in Es)
            {
                list.Add(new Translation("es", key, value));
            }
            return list;
        }
    }

    // -------------------------------------------------------------------
    // English
    // -------------------------------------------------------------------
    private static readonly Dictionary<string, string> En = new(StringComparer.Ordinal)
    {
        // Common / shared
        ["Common_Loading"] = "Loading…",
        ["Common_Cancel"] = "Cancel",
        ["Common_Apply"] = "Apply",
        ["Common_Close"] = "Close",
        ["Common_Back"] = "Back",
        ["Common_ViewDetails"] = "View details",
        ["Common_ViewAll"] = "View all",
        ["Common_Search"] = "Search",
        ["Common_Sort"] = "Sort",
        ["Common_Clear"] = "Clear",
        ["Common_SelectAll"] = "Select all",
        ["Common_ClearAll"] = "Clear all",
        ["Common_SearchOff"] = "No results",
        ["Common_Verified"] = "Verified",

        // Layout / navigation
        ["Nav_Map"] = "Map",
        ["Nav_Species"] = "Species",
        ["Nav_Municipios"] = "Municipios",
        ["Nav_Categories"] = "Categories",

        // Hero (shared eyebrow)
        ["Hero_Eyebrow"] = "Volume 03 · Living Atlas",
        ["Hero_LastSync"] = "Last sync · {0}",

        // Species page — hero
        ["Species_PageTitle"] = "FaunaFinder — Species",
        ["Species_Hero_HeadingLead"] = "Species of",
        ["Species_Hero_HeadingTail"] = ", catalogued and observed.",
        ["Species_Hero_Lede"] = "A living field guide to the island's flora and fauna — drawn from verified sightings across 78 municipios. Browse by taxon, conservation status, or habitat; follow any record through to its distribution map and source observations.",
        ["Species_Hero_RecordsLine"] = "{0} records · {1} municipios",
        ["Species_Hero_UpdatedDaily"] = "Updated daily",
        ["Species_Hero_RecordsPending"] = "— records · — municipios",

        // Species page — stats row
        ["Species_Stats_Total_Label"] = "Total species",
        ["Species_Stats_Total_Sub_Delta"] = "{0} this quarter",
        ["Species_Stats_Total_Sub_None"] = "—",
        ["Species_Stats_Endemic_Label"] = "Endemic to P.R.",
        ["Species_Stats_Endemic_Sub"] = "{0}% of catalogue",
        ["Species_Stats_Threatened_Label"] = "Threatened · VU–CR",
        ["Species_Stats_Threatened_Sub_Delta"] = "{0} reclassified",
        ["Species_Stats_Municipios_Label"] = "Municipios covered",
        ["Species_Stats_Municipios_Sub"] = "{0}% island coverage",

        // Species page — featured row
        ["Species_Featured_Heading"] = "Featured this week",
        ["Species_Featured_Meta"] = "Curated · updated Mondays",

        // Species page — toolbar
        ["Species_Toolbar_Heading"] = "Find what you're looking for",
        ["Species_Toolbar_RecordsMeta"] = "{0} records · {1} municipios",
        ["Species_Toolbar_RecordsMeta_Loading"] = "Loading catalogue…",
        ["Species_Toolbar_SearchPlaceholder"] = "Search by common name, scientific name, or species…",
        ["Species_Toolbar_Counter_Of"] = "of {0}",
        ["Species_Toolbar_Sort_Prefix"] = "Sort:",
        ["Species_Toolbar_Sort_ScientificAsc"] = "Scientific name ↑",
        ["Species_Toolbar_Sort_ScientificDesc"] = "Scientific name ↓",
        ["Species_Toolbar_Sort_Recent"] = "Recently observed",
        ["Species_Toolbar_Sort_MostWidespread"] = "Most widespread",

        // Species page — active-filter chips
        ["Species_Chip_Taxon"] = "Taxon: {0}",
        ["Species_Chip_Status"] = "Status: {0}",
        ["Species_Chip_Endemic"] = "Endemic only",
        ["Species_Chip_ObservedRecently"] = "Observed in last 12 months",
        ["Species_Chip_HasPhoto"] = "Has photo",
        ["Species_Chip_MinMunicipios"] = "≥ {0} municipios",

        // Species page — filter dialog
        ["Species_Filter_Heading"] = "Refine the catalogue",
        ["Species_Filter_TotalRecords"] = "{0} records",
        ["Species_Filter_Section_Taxa"] = "Taxonomic group",
        ["Species_Filter_Section_Status"] = "Conservation status (IUCN)",
        ["Species_Filter_Section_Qualifiers"] = "Qualifiers",
        ["Species_Filter_Section_MinMunicipios"] = "Minimum municipios present",
        ["Species_Filter_Qualifier_EndemicLabel"] = "Endemic to Puerto Rico only",
        ["Species_Filter_Qualifier_ObservedLabel"] = "Observed in the last 12 months",
        ["Species_Filter_Qualifier_HasPhotoLabel"] = "Has photo",
        ["Species_Filter_StatusMeta_Selected"] = "{0} selected",
        ["Species_Filter_MinMunicipios_Meta"] = "≥ {0}",
        ["Species_Filter_MinMunicipios_Rare"] = "1 (rare)",
        ["Species_Filter_MinMunicipios_Ubiquitous"] = "78 (ubiquitous)",
        ["Species_Filter_Reset"] = "Reset all filters",

        // Species page — taxa labels (chip + filter grid)
        ["Species_Taxa_Bird"] = "Bird",
        ["Species_Taxa_Plant"] = "Plant",
        ["Species_Taxa_Reptile"] = "Reptile",
        ["Species_Taxa_Amphib"] = "Amphibian",
        ["Species_Taxa_Fish"] = "Fish",
        ["Species_Taxa_Mammal"] = "Mammal",
        ["Species_Taxa_Invert"] = "Invertebrate",
        ["Species_Taxa_Fungi"] = "Fungus",

        // IUCN conservation status labels (full text — short codes stay as-is)
        ["Species_Iucn_LC"] = "Least Concern",
        ["Species_Iucn_NT"] = "Near Threatened",
        ["Species_Iucn_VU"] = "Vulnerable",
        ["Species_Iucn_EN"] = "Endangered",
        ["Species_Iucn_CR"] = "Critically Endangered",
        ["Species_Iucn_DD"] = "Data Deficient",
        ["Species_Iucn_EX"] = "Extinct",

        // Species card / grid
        ["Species_Card_Municipios"] = "{0} municipios",
        ["Species_Card_EndemicBadge"] = "Endemic PR",
        ["Species_Grid_LoadingMore"] = "Loading more records…",
        ["Species_Grid_LoadMore"] = "Load more records",
        ["Species_Grid_EndOfCatalogue"] = "— end of catalogue —",
        ["Species_Grid_Empty_Title"] = "No species match",
        ["Species_Grid_Empty_Description"] = "Try adjusting your filters or search terms.",

        // Species card — relative time
        ["Time_MinutesAgo"] = "{0}m ago",
        ["Time_HoursAgo"] = "{0}h ago",
        ["Time_DaysAgo"] = "{0}d ago",
        ["Time_MonthsAgo"] = "{0}mo ago",
        ["Time_YearsAgo"] = "{0}y ago",

        // Species detail
        ["SpeciesDetail_Rank_Global"] = "Global: {0}",
        ["SpeciesDetail_Rank_State"] = "State: {0}",
        ["SpeciesDetail_Kingdom_Fauna"] = "Fauna",
        ["SpeciesDetail_Kingdom_Flora"] = "Flora",
        ["SpeciesDetail_ElCode"] = "Element Code: {0}",
        ["SpeciesDetail_Categories_Heading"] = "Categories",
        ["SpeciesDetail_Municipalities_Heading"] = "Municipalities",
        ["SpeciesDetail_Municipalities_Empty_Title"] = "No municipalities",
        ["SpeciesDetail_Municipalities_Empty_Description"] = "This species has no recorded municipality associations yet.",
        ["SpeciesDetail_NotFound_Title"] = "Species not found",
        ["SpeciesDetail_NotFound_Description"] = "The species you're looking for doesn't exist or has been removed.",

        // Municipalities page — hero
        ["Muni_PageTitle"] = "FaunaFinder — Municipios",
        ["Muni_Hero_HeadingLead"] = "Municipios of",
        ["Muni_Hero_HeadingTail"] = ", mapped and observed.",
        ["Muni_Hero_Lede"] = "A geographic index to Puerto Rico's 78 municipios — each annotated with the species recorded inside its boundaries. Search by name, sort by biodiversity, or tap any pin to pull up a municipio's full roster and its notable residents.",
        ["Muni_Hero_RecordsLine"] = "{0} municipios · {1} records",
        ["Muni_Hero_RecordsPending"] = "— municipios · — records",
        ["Muni_Hero_Hint"] = "Tap a pin or row to drill in",

        // Municipalities page — stats row
        ["Muni_Stats_Total_Label"] = "Total municipios",
        ["Muni_Stats_Total_Unit"] = "/ {0} tracked",
        ["Muni_Stats_Total_Sub"] = "100% island coverage",
        ["Muni_Stats_Species_Label"] = "Species catalogued",
        ["Muni_Stats_Species_Sub"] = "across the island",
        ["Muni_Stats_Avg_Label"] = "Avg · species / municipio",
        ["Muni_Stats_Avg_Sub"] = "{0} municipios contribute",
        ["Muni_Stats_Hotspots_Label"] = "Endemic hotspots",
        ["Muni_Stats_Hotspots_Sub"] = "≥ 10 endemic species present",

        // Municipalities — tab pill
        ["Muni_Tab_Map"] = "Map",
        ["Muni_Tab_List"] = "List",

        // Municipalities — list
        ["Muni_List_SearchPlaceholder"] = "Search municipios by name…",
        ["Muni_List_VisibleAll"] = "{0} municipios",
        ["Muni_List_VisibleFiltered"] = "{0} of {1}",
        ["Muni_List_Loading"] = "Loading…",
        ["Muni_List_Sort_SpeciesCountDesc"] = "species count ↓",
        ["Muni_List_Sort_NameAsc"] = "name A–Z",
        ["Muni_List_Sort_Prefix"] = "Sort:",
        ["Muni_List_Empty"] = "No municipios match \"{0}\"",
        ["Muni_List_Foot_Loading"] = "loading municipios",
        ["Muni_List_Foot_Total"] = "{0} municipios tracked",

        // Municipalities — map legend + detail card
        ["Muni_Map_Legend_Title"] = "Municipios",
        ["Muni_Map_Legend_Selected"] = "Selected",
        ["Muni_Map_Legend_Boundary"] = "Boundary",
        ["Muni_DetailCard_Species_Label"] = "Species",
        ["Muni_DetailCard_Fips_Label"] = "FIPS",
        ["Muni_DetailCard_CountRecorded"] = "{0} species recorded",
        ["Muni_DetailCard_CountPending"] = "Species count pending",
        ["Muni_Card_SpeciesCount"] = "{0} species",

        // Municipalities — detail route
        ["MuniDetail_Subline"] = "{0} · FIPS {1}",
        ["MuniDetail_Centroid"] = "Centroid · {0}″N {1}″W",
        ["MuniDetail_Species_Heading"] = "Species",
        ["MuniDetail_Species_Empty_Title"] = "No species recorded",
        ["MuniDetail_Species_Empty_Description"] = "No species have been associated with this municipio yet.",
        ["MuniDetail_NotFound_Title"] = "Municipio not found",
        ["MuniDetail_NotFound_Description"] = "The municipio you're looking for doesn't exist or has been removed.",

        // Categories page
        ["Categories_PageTitle"] = "Categories",
        ["Categories_Empty_Title"] = "No categories",
        ["Categories_Empty_Description"] = "No species categories have been created yet.",
        ["CategoryDetail_SpeciesCount"] = "{0} Species",
        ["CategoryDetail_Empty_Title"] = "No species",
        ["CategoryDetail_Empty_Description"] = "No species have been categorized here yet.",

        // Home (map page)
        ["Home_MapLoading"] = "Loading map…",
        ["Home_SidebarEmpty"] = "Tap a municipio to see its species",
        ["Map_Panel_Eyebrow"] = "Municipio",
        ["Map_Panel_NoSpecies_Title"] = "No species recorded",
        ["Map_Panel_NoSpecies_Description"] = "No species have been observed in this municipio yet.",
        ["Map_Panel_SpeciesObserved"] = "{0} species observed",
    };

    // -------------------------------------------------------------------
    // Spanish
    // -------------------------------------------------------------------
    private static readonly Dictionary<string, string> Es = new(StringComparer.Ordinal)
    {
        // Common / shared
        ["Common_Loading"] = "Cargando…",
        ["Common_Cancel"] = "Cancelar",
        ["Common_Apply"] = "Aplicar",
        ["Common_Close"] = "Cerrar",
        ["Common_Back"] = "Atrás",
        ["Common_ViewDetails"] = "Ver detalles",
        ["Common_ViewAll"] = "Ver todos",
        ["Common_Search"] = "Buscar",
        ["Common_Sort"] = "Ordenar",
        ["Common_Clear"] = "Limpiar",
        ["Common_SelectAll"] = "Seleccionar todos",
        ["Common_ClearAll"] = "Limpiar todos",
        ["Common_SearchOff"] = "Sin resultados",
        ["Common_Verified"] = "Verificado",

        // Layout / navigation
        ["Nav_Map"] = "Mapa",
        ["Nav_Species"] = "Especies",
        ["Nav_Municipios"] = "Municipios",
        ["Nav_Categories"] = "Categorías",

        // Hero
        ["Hero_Eyebrow"] = "Volumen 03 · Atlas Viviente",
        ["Hero_LastSync"] = "Última sincronización · {0}",

        // Species page — hero
        ["Species_PageTitle"] = "FaunaFinder — Especies",
        ["Species_Hero_HeadingLead"] = "Especies de",
        ["Species_Hero_HeadingTail"] = ", catalogadas y observadas.",
        ["Species_Hero_Lede"] = "Una guía de campo viva de la flora y fauna de la isla — construida con avistamientos verificados en los 78 municipios. Explora por taxón, estado de conservación o hábitat; sigue cualquier registro hasta su mapa de distribución y observaciones originales.",
        ["Species_Hero_RecordsLine"] = "{0} registros · {1} municipios",
        ["Species_Hero_UpdatedDaily"] = "Actualizado a diario",
        ["Species_Hero_RecordsPending"] = "— registros · — municipios",

        // Species page — stats row
        ["Species_Stats_Total_Label"] = "Total de especies",
        ["Species_Stats_Total_Sub_Delta"] = "{0} este trimestre",
        ["Species_Stats_Total_Sub_None"] = "—",
        ["Species_Stats_Endemic_Label"] = "Endémicas de P.R.",
        ["Species_Stats_Endemic_Sub"] = "{0}% del catálogo",
        ["Species_Stats_Threatened_Label"] = "Amenazadas · VU–CR",
        ["Species_Stats_Threatened_Sub_Delta"] = "{0} reclasificadas",
        ["Species_Stats_Municipios_Label"] = "Municipios cubiertos",
        ["Species_Stats_Municipios_Sub"] = "{0}% de la isla cubierta",

        // Species page — featured row
        ["Species_Featured_Heading"] = "Destacadas esta semana",
        ["Species_Featured_Meta"] = "Curado · actualizado los lunes",

        // Species page — toolbar
        ["Species_Toolbar_Heading"] = "Encuentra lo que buscas",
        ["Species_Toolbar_RecordsMeta"] = "{0} registros · {1} municipios",
        ["Species_Toolbar_RecordsMeta_Loading"] = "Cargando catálogo…",
        ["Species_Toolbar_SearchPlaceholder"] = "Busca por nombre común, nombre científico o especie…",
        ["Species_Toolbar_Counter_Of"] = "de {0}",
        ["Species_Toolbar_Sort_Prefix"] = "Orden:",
        ["Species_Toolbar_Sort_ScientificAsc"] = "Nombre científico ↑",
        ["Species_Toolbar_Sort_ScientificDesc"] = "Nombre científico ↓",
        ["Species_Toolbar_Sort_Recent"] = "Observadas recientemente",
        ["Species_Toolbar_Sort_MostWidespread"] = "Más extendidas",

        // Species page — active-filter chips
        ["Species_Chip_Taxon"] = "Taxón: {0}",
        ["Species_Chip_Status"] = "Estado: {0}",
        ["Species_Chip_Endemic"] = "Solo endémicas",
        ["Species_Chip_ObservedRecently"] = "Observadas en los últimos 12 meses",
        ["Species_Chip_HasPhoto"] = "Con foto",
        ["Species_Chip_MinMunicipios"] = "≥ {0} municipios",

        // Species page — filter dialog
        ["Species_Filter_Heading"] = "Refinar el catálogo",
        ["Species_Filter_TotalRecords"] = "{0} registros",
        ["Species_Filter_Section_Taxa"] = "Grupo taxonómico",
        ["Species_Filter_Section_Status"] = "Estado de conservación (UICN)",
        ["Species_Filter_Section_Qualifiers"] = "Criterios",
        ["Species_Filter_Section_MinMunicipios"] = "Mínimo de municipios",
        ["Species_Filter_Qualifier_EndemicLabel"] = "Solo endémicas de Puerto Rico",
        ["Species_Filter_Qualifier_ObservedLabel"] = "Observadas en los últimos 12 meses",
        ["Species_Filter_Qualifier_HasPhotoLabel"] = "Con foto",
        ["Species_Filter_StatusMeta_Selected"] = "{0} seleccionadas",
        ["Species_Filter_MinMunicipios_Meta"] = "≥ {0}",
        ["Species_Filter_MinMunicipios_Rare"] = "1 (raras)",
        ["Species_Filter_MinMunicipios_Ubiquitous"] = "78 (ubicuas)",
        ["Species_Filter_Reset"] = "Restablecer filtros",

        // Species page — taxa labels
        ["Species_Taxa_Bird"] = "Ave",
        ["Species_Taxa_Plant"] = "Planta",
        ["Species_Taxa_Reptile"] = "Reptil",
        ["Species_Taxa_Amphib"] = "Anfibio",
        ["Species_Taxa_Fish"] = "Pez",
        ["Species_Taxa_Mammal"] = "Mamífero",
        ["Species_Taxa_Invert"] = "Invertebrado",
        ["Species_Taxa_Fungi"] = "Hongo",

        // IUCN conservation status labels
        ["Species_Iucn_LC"] = "Preocupación menor",
        ["Species_Iucn_NT"] = "Casi amenazada",
        ["Species_Iucn_VU"] = "Vulnerable",
        ["Species_Iucn_EN"] = "En peligro",
        ["Species_Iucn_CR"] = "En peligro crítico",
        ["Species_Iucn_DD"] = "Datos insuficientes",
        ["Species_Iucn_EX"] = "Extinta",

        // Species card / grid
        ["Species_Card_Municipios"] = "{0} municipios",
        ["Species_Card_EndemicBadge"] = "Endémica PR",
        ["Species_Grid_LoadingMore"] = "Cargando más registros…",
        ["Species_Grid_LoadMore"] = "Cargar más registros",
        ["Species_Grid_EndOfCatalogue"] = "— fin del catálogo —",
        ["Species_Grid_Empty_Title"] = "Sin coincidencias",
        ["Species_Grid_Empty_Description"] = "Ajusta los filtros o los términos de búsqueda.",

        // Relative time
        ["Time_MinutesAgo"] = "hace {0} min",
        ["Time_HoursAgo"] = "hace {0} h",
        ["Time_DaysAgo"] = "hace {0} d",
        ["Time_MonthsAgo"] = "hace {0} meses",
        ["Time_YearsAgo"] = "hace {0} años",

        // Species detail
        ["SpeciesDetail_Rank_Global"] = "Global: {0}",
        ["SpeciesDetail_Rank_State"] = "Estatal: {0}",
        ["SpeciesDetail_Kingdom_Fauna"] = "Fauna",
        ["SpeciesDetail_Kingdom_Flora"] = "Flora",
        ["SpeciesDetail_ElCode"] = "Código de elemento: {0}",
        ["SpeciesDetail_Categories_Heading"] = "Categorías",
        ["SpeciesDetail_Municipalities_Heading"] = "Municipios",
        ["SpeciesDetail_Municipalities_Empty_Title"] = "Sin municipios",
        ["SpeciesDetail_Municipalities_Empty_Description"] = "Esta especie aún no tiene municipios asociados.",
        ["SpeciesDetail_NotFound_Title"] = "Especie no encontrada",
        ["SpeciesDetail_NotFound_Description"] = "La especie que buscas no existe o fue eliminada.",

        // Municipalities page — hero
        ["Muni_PageTitle"] = "FaunaFinder — Municipios",
        ["Muni_Hero_HeadingLead"] = "Municipios de",
        ["Muni_Hero_HeadingTail"] = ", en el mapa y observados.",
        ["Muni_Hero_Lede"] = "Un índice geográfico de los 78 municipios de Puerto Rico — cada uno anotado con las especies registradas dentro de sus límites. Busca por nombre, ordena por biodiversidad, o toca un pin para abrir el registro completo del municipio y sus residentes más destacados.",
        ["Muni_Hero_RecordsLine"] = "{0} municipios · {1} registros",
        ["Muni_Hero_RecordsPending"] = "— municipios · — registros",
        ["Muni_Hero_Hint"] = "Toca un pin o una fila para profundizar",

        // Municipalities page — stats row
        ["Muni_Stats_Total_Label"] = "Total de municipios",
        ["Muni_Stats_Total_Unit"] = "/ {0} rastreados",
        ["Muni_Stats_Total_Sub"] = "100% de la isla",
        ["Muni_Stats_Species_Label"] = "Especies catalogadas",
        ["Muni_Stats_Species_Sub"] = "en toda la isla",
        ["Muni_Stats_Avg_Label"] = "Prom · especies / municipio",
        ["Muni_Stats_Avg_Sub"] = "{0} municipios aportan",
        ["Muni_Stats_Hotspots_Label"] = "Focos endémicos",
        ["Muni_Stats_Hotspots_Sub"] = "≥ 10 especies endémicas presentes",

        // Municipalities — tab pill
        ["Muni_Tab_Map"] = "Mapa",
        ["Muni_Tab_List"] = "Lista",

        // Municipalities — list
        ["Muni_List_SearchPlaceholder"] = "Busca municipios por nombre…",
        ["Muni_List_VisibleAll"] = "{0} municipios",
        ["Muni_List_VisibleFiltered"] = "{0} de {1}",
        ["Muni_List_Loading"] = "Cargando…",
        ["Muni_List_Sort_SpeciesCountDesc"] = "cantidad de especies ↓",
        ["Muni_List_Sort_NameAsc"] = "nombre A–Z",
        ["Muni_List_Sort_Prefix"] = "Orden:",
        ["Muni_List_Empty"] = "Ningún municipio coincide con \"{0}\"",
        ["Muni_List_Foot_Loading"] = "cargando municipios",
        ["Muni_List_Foot_Total"] = "{0} municipios rastreados",

        // Municipalities — map legend + detail card
        ["Muni_Map_Legend_Title"] = "Municipios",
        ["Muni_Map_Legend_Selected"] = "Seleccionado",
        ["Muni_Map_Legend_Boundary"] = "Límite",
        ["Muni_DetailCard_Species_Label"] = "Especies",
        ["Muni_DetailCard_Fips_Label"] = "FIPS",
        ["Muni_DetailCard_CountRecorded"] = "{0} especies registradas",
        ["Muni_DetailCard_CountPending"] = "Conteo pendiente",
        ["Muni_Card_SpeciesCount"] = "{0} especies",

        // Municipalities — detail route
        ["MuniDetail_Subline"] = "{0} · FIPS {1}",
        ["MuniDetail_Centroid"] = "Centroide · {0}″N {1}″O",
        ["MuniDetail_Species_Heading"] = "Especies",
        ["MuniDetail_Species_Empty_Title"] = "Sin especies",
        ["MuniDetail_Species_Empty_Description"] = "Aún no se han asociado especies con este municipio.",
        ["MuniDetail_NotFound_Title"] = "Municipio no encontrado",
        ["MuniDetail_NotFound_Description"] = "El municipio que buscas no existe o fue eliminado.",

        // Categories page
        ["Categories_PageTitle"] = "Categorías",
        ["Categories_Empty_Title"] = "Sin categorías",
        ["Categories_Empty_Description"] = "Aún no se han creado categorías de especies.",
        ["CategoryDetail_SpeciesCount"] = "{0} especies",
        ["CategoryDetail_Empty_Title"] = "Sin especies",
        ["CategoryDetail_Empty_Description"] = "Aún no se ha categorizado ninguna especie aquí.",

        // Home (map page)
        ["Home_MapLoading"] = "Cargando mapa…",
        ["Home_SidebarEmpty"] = "Toca un municipio para ver sus especies",
        ["Map_Panel_Eyebrow"] = "Municipio",
        ["Map_Panel_NoSpecies_Title"] = "Sin especies registradas",
        ["Map_Panel_NoSpecies_Description"] = "Aún no se han observado especies en este municipio.",
        ["Map_Panel_SpeciesObserved"] = "{0} especies observadas",
    };
}
