namespace EcoData.Common.Http.Helpers;

public sealed class QueryStringBuilder
{
    private readonly List<string> _parameters = [];

    public QueryStringBuilder Add(string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            _parameters.Add($"{key}={Uri.EscapeDataString(value)}");
        }

        return this;
    }

    public QueryStringBuilder Add(string key, int? value)
    {
        if (value.HasValue)
        {
            _parameters.Add($"{key}={value.Value}");
        }

        return this;
    }

    public QueryStringBuilder Add(string key, Guid? value)
    {
        if (value.HasValue)
        {
            _parameters.Add($"{key}={value.Value}");
        }

        return this;
    }

    public QueryStringBuilder Add(string key, bool? value)
    {
        if (value.HasValue)
        {
            _parameters.Add($"{key}={value.Value.ToString().ToLowerInvariant()}");
        }

        return this;
    }

    public string Build()
    {
        return _parameters.Count > 0 ? $"?{string.Join("&", _parameters)}" : string.Empty;
    }
}
