using EcoData.Spa.Blazor;
using EcoData.Wildlife.Contracts.Parameters;
using FaunaFinder.Client.Localization;
using Tempest;

namespace FaunaFinder.Client.Components.Municipalities;

// The [Command] lives in this code-behind (with the base stated explicitly) because
// Tempest's razor frontend matches the @inherits text by simple name and can't see
// that EcoDataComponent is a StatefulComponent; the C# symbol frontend can.
public partial class MunicipalityCard : EcoDataComponent
{
    private string? _countLoadError;

    [Command, RunOnLoad]
    private async Task<int> LoadCount(CancellationToken ct)
    {
        var result = await SpeciesClient.GetCountAsync(
            new SpeciesParameters(MunicipalityId: Municipality.Id),
            ct);
        if (result.TryPickT0(out var count, out var error))
        {
            _countLoadError = null;
            return count;
        }
        _countLoadError = error.StatusCode == 0
            ? "Couldn't reach the server."
            : error.Message ?? "Couldn't load the species count.";
        return 0;
    }
}
