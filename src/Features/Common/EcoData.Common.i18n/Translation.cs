namespace EcoData.Common.i18n;

/// <summary>
/// Simple value-type implementation of <see cref="ITranslation"/>.
/// </summary>
public sealed record Translation(string LanguageCode, string Key, string Value) : ITranslation;
