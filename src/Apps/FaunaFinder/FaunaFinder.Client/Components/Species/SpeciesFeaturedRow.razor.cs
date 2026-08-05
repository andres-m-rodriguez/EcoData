using EcoData.Wildlife.Contracts.Dtos;
using FaunaFinder.Client.Localization;
using Tempest;

namespace FaunaFinder.Client.Components.Species;

// The [Command] lives in this code-behind (with the base stated explicitly) because
// Tempest's razor frontend matches the @inherits text by simple name and can't see
// that LocalizedComponentBase is a StatefulComponent; the C# symbol frontend can.
public partial class SpeciesFeaturedRow : LocalizedComponentBase
{
    private string? _loadError;

    [Command, RunOnLoad]
    private async Task<IReadOnlyList<SpeciesDtoForList>> LoadFeatured(CancellationToken ct)
    {
        var result = await SpeciesClient.GetFeaturedAsync(ct);
        if (result.TryPickT0(out var featured, out var error))
        {
            _loadError = null;
            return featured;
        }
        _loadError = error.StatusCode == 0
            ? "Couldn't reach the server."
            : error.Message ?? "Something went wrong while loading featured species.";
        return [];
    }
}
