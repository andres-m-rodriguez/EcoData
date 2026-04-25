namespace EcoData.Organization.DataAccess.Colors;

public static class HexColor
{
    public const int StorageLength = 7;

    // Stores brand colors as 7-char "#rrggbb". Accepts inputs with or without
    // the leading hash; rejects anything else by returning null. Keeps malformed
    // input out of CSS where it would silently break the org-themed page.
    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var trimmed = value.Trim();
        var body = trimmed.StartsWith('#') ? trimmed[1..] : trimmed;
        if (body.Length != 6) return null;

        foreach (var c in body)
        {
            if (!Uri.IsHexDigit(c)) return null;
        }

        return "#" + body.ToLowerInvariant();
    }
}
