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

    public QueryStringBuilder Add(string key, decimal? value)
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

    public QueryStringBuilder Add(string key, DateTimeOffset? value)
    {
        if (value.HasValue)
        {
            _parameters.Add($"{key}={Uri.EscapeDataString(value.Value.ToString("o"))}");
        }

        return this;
    }

    public QueryStringBuilder Add<T>(string key, IReadOnlyList<T>? values)
    {
        if (values is null || values.Count == 0)
        {
            return this;
        }

        foreach (var value in values)
        {
            if (value is null)
            {
                continue;
            }

            var text = value.ToString();
            if (string.IsNullOrEmpty(text))
            {
                continue;
            }

            _parameters.Add($"{key}={Uri.EscapeDataString(text)}");
        }

        return this;
    }

    public QueryStringBuilder Add<TEnum>(string key, TEnum? value)
        where TEnum : struct, Enum
    {
        if (value.HasValue)
        {
            _parameters.Add($"{key}={Uri.EscapeDataString(value.Value.ToString())}");
        }

        return this;
    }

    public string Build()
    {
        return _parameters.Count > 0 ? $"?{string.Join("&", _parameters)}" : string.Empty;
    }
}
