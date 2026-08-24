namespace EcoData.Organization.DataAccess.Colors;

public static class HexColor
{
    public const int StorageLength = 7;

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
