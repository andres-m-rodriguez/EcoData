namespace FaunaFinder.Client.Services.FieldNotebook;

/// <summary>What kind of thing a notebook entry points at.</summary>
public enum NotebookEntryKind
{
    Species,
    Municipality,
}

/// <summary>
/// One line in the field notebook.
///
/// <para>The labels are a snapshot taken when the entry was written, not a
/// live projection — the notebook has to render without refetching every
/// row, and a saved entry should survive the record it points at changing.
/// The trade is that a language switch leaves <see cref="Label"/> in the
/// language it was saved in until the entry is visited again;
/// <see cref="Sublabel"/> carries the scientific name for species, which is
/// language-neutral.</para>
/// </summary>
/// <param name="HasImage">
/// Whether the thing has a profile photograph to show. Species serve theirs
/// from <c>/wildlife/species/{id}/image</c>; municipios have none. Trailing
/// and defaulted on purpose — entries written before this existed deserialize
/// with it false rather than failing the whole blob.
/// </param>
public sealed record NotebookEntry(
    NotebookEntryKind Kind,
    Guid Id,
    string Label,
    string? Sublabel,
    string? Status,
    DateTimeOffset RecordedAtUtc,
    bool HasImage = false
);
