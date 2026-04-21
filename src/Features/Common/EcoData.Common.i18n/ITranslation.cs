namespace EcoData.Common.i18n;

/// <summary>
/// Contract for a translation entry.
/// </summary>
public interface ITranslation
{
    /// <summary>
    /// The language code this translation belongs to.
    /// </summary>
    string LanguageCode { get; }

    /// <summary>
    /// The translation key (e.g., "Common_Loading").
    /// </summary>
    string Key { get; }

    /// <summary>
    /// The translated value (e.g., "Loading...").
    /// </summary>
    string Value { get; }
}
