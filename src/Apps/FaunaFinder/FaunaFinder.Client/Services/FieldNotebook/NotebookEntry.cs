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
public sealed record NotebookEntry(
    NotebookEntryKind Kind,
    Guid Id,
    string Label,
    string? Sublabel,
    string? Status,
    DateTimeOffset RecordedAtUtc
);
