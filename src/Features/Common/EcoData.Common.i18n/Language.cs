namespace EcoData.Common.i18n;

/// <summary>
/// Simple value-type implementation of <see cref="ILanguage"/>.
/// </summary>
public sealed record Language(string Code, string Name, bool IsDefault = false) : ILanguage;
