using EcoData.Locations.DataAccess.Interfaces;
using EcoData.Wildlife.DataAccess.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace FaunaFinder.Server.Reports;

public static class SpeciesReportEndpoints
{
    public static IEndpointRouteBuilder MapSpeciesReportEndpoints(this IEndpointRouteBuilder app)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        app.MapGet(
                "/reports/species/{id:guid}.pdf",
                async Task<Results<FileContentHttpResult, NotFound>> (
                    Guid id,
                    string? lang,
                    ISpeciesRepository speciesRepository,
                    IConservationLinkRepository linkRepository,
                    IMunicipalityRepository municipalityRepository,
                    CancellationToken ct
                ) =>
                {
                    var species = await speciesRepository.GetByIdAsync(id, ct);
                    if (species is null)
                    {
                        return TypedResults.NotFound();
                    }

                    var image = species.HasProfileImage
                        ? await speciesRepository.GetProfileImageAsync(id, ct)
                        : null;
                    var links = await linkRepository.GetBySpeciesAsync(id, ct);
                    var municipalities = await municipalityRepository.GetByIdsAsync(
                        species.MunicipalityIds,
                        ct
                    );

                    var document = new SpeciesReportDocument(
                        species,
                        image,
                        links.Links,
                        municipalities.Select(m => m.Name).Order().ToList(),
                        SpeciesReportLabels.For(lang)
                    );

                    var fileName = $"{species.ScientificName.Replace(' ', '-')}.pdf";
                    return TypedResults.File(document.GeneratePdf(), "application/pdf", fileName);
                }
            )
            .WithName("GetSpeciesReport")
            .WithTags("Species");

        return app;
    }
}
