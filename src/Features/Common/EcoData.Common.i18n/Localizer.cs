using System.Globalization;

namespace EcoData.Common.i18n;

/// <summary>
/// Concrete in-memory <see cref="ILocalizer"/>. Translations are registered
/// at construction time; missing keys return the key itself so developers
/// can spot gaps without the UI crashing.
/// </summary>
public sealed class Localizer : ILocalizer
{
    private readonly IReadOnlyList<ILanguage> _languages;
    private readonly Dictionary<string, Dictionary<string, string>> _byLanguage;
    private readonly string _defaultLanguage;

    public string CurrentLanguage { get; private set; }

    /// <summary>
    /// Fired whenever <see cref="SetLanguage"/> actually changes the locale.
    /// Components subscribe to this to re-render localized content.
    /// </summary>
    public event Action? LanguageChanged;

    public Localizer(
        IReadOnlyList<ILanguage> languages,
        IReadOnlyList<ITranslation> translations)
    {
        if (languages.Count == 0)
        {
            throw new ArgumentException("At least one language must be provided.", nameof(languages));
        }

        _languages = languages;
        _defaultLanguage = languages.FirstOrDefault(l => l.IsDefault)?.Code ?? languages[0].Code;
        CurrentLanguage = _defaultLanguage;

        _byLanguage = languages.ToDictionary(
            l => l.Code,
            _ => new Dictionary<string, string>(StringComparer.Ordinal),
            StringComparer.OrdinalIgnoreCase);

        foreach (var t in translations)
        {
            if (_byLanguage.TryGetValue(t.LanguageCode, out var bucket))
            {
                bucket[t.Key] = t.Value;
            }
        }
    }

    public string this[string key] => Resolve(key);

    public string this[string key, params object[] args] =>
        string.Format(CultureInfo.CurrentCulture, Resolve(key), args);

    public void SetLanguage(string languageCode)
    {
        if (string.Equals(languageCode, CurrentLanguage, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
        if (!_byLanguage.ContainsKey(languageCode))
        {
            return;
        }
        CurrentLanguage = languageCode;
        LanguageChanged?.Invoke();
    }

    public IReadOnlyList<ILanguage> GetAvailableLanguages() => _languages;

    private string Resolve(string key)
    {
        if (_byLanguage.TryGetValue(CurrentLanguage, out var bucket)
            && bucket.TryGetValue(key, out var value))
        {
            return value;
        }

        // Fall back to the default language before surfacing the raw key.
        if (!string.Equals(CurrentLanguage, _defaultLanguage, StringComparison.OrdinalIgnoreCase)
            && _byLanguage.TryGetValue(_defaultLanguage, out var defaultBucket)
            && defaultBucket.TryGetValue(key, out var defaultValue))
        {
            return defaultValue;
        }

        return key;
    }
}
