namespace EcoData.Common.i18n;

/// <summary>
/// Represents a localized string value with a language code.
/// Used for storing multilingual content in database entities.
/// </summary>
/// <param name="Code">The language/locale code (e.g., "en", "es", "en-US")</param>
/// <param name="Value">The localized string value</param>
public sealed record LocaleValue(string Code, string Value);
