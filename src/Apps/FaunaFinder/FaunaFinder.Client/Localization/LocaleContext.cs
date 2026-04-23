using EcoData.Common.i18n;

namespace FaunaFinder.Client.Localization;

public sealed record LocaleContext(string Code)
{
    public static LocaleContext English { get; } = new("en");
    public static LocaleContext Spanish { get; } = new("es");

    public string Resolve(IReadOnlyList<LocaleValue>? values, string? fallback = null)
    {
        if (values is null || values.Count == 0)
        {
            return fallback ?? string.Empty;
        }

        var match = values.FirstOrDefault(v => v.Code == Code);
        if (match is not null)
        {
            return match.Value;
        }

        var english = values.FirstOrDefault(v => v.Code == "en");
        return english?.Value ?? values[0].Value ?? fallback ?? string.Empty;
    }
}
