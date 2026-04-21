namespace EcoData.Common.i18n;

/// <summary>
/// Contract for a localization service that provides translated strings.
/// </summary>
public interface ILocalizer
{
    /// <summary>
    /// Gets the current language code.
    /// </summary>
    string CurrentLanguage { get; }

    /// <summary>
    /// Gets a translated string by key.
    /// </summary>
    /// <param name="key">The translation key.</param>
    /// <returns>The translated string, or the key if not found.</returns>
    string this[string key] { get; }

    /// <summary>
    /// Gets a translated string by key with format arguments.
    /// </summary>
    /// <param name="key">The translation key.</param>
    /// <param name="args">Format arguments.</param>
    /// <returns>The formatted translated string.</returns>
    string this[string key, params object[] args] { get; }

    /// <summary>
    /// Sets the current language.
    /// </summary>
    /// <param name="languageCode">The language code to switch to.</param>
    void SetLanguage(string languageCode);

    /// <summary>
    /// Gets all available languages.
    /// </summary>
    IReadOnlyList<ILanguage> GetAvailableLanguages();
}
