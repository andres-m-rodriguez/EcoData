using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace EcoData.Organization.DataAccess.Slugs;

public static partial class SlugGenerator
{
    public const int MaxLength = 80;

    // Strips diacritics (é → e), lowercases, collapses any run of
    // non-alphanumeric characters into a single hyphen, trims leading/trailing
    // hyphens, and truncates to MaxLength so it always fits the column.
    public static string FromName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var stripped = StripDiacritics(name).ToLowerInvariant();
        var hyphenated = NonSlugCharsRegex().Replace(stripped, "-").Trim('-');

        return hyphenated.Length > MaxLength ? hyphenated[..MaxLength].TrimEnd('-') : hyphenated;
    }

    private static string StripDiacritics(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(ch);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonSlugCharsRegex();
}
