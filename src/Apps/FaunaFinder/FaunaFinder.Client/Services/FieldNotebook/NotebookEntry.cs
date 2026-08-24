namespace FaunaFinder.Client.Services.FieldNotebook;

public enum NotebookEntryKind
{
    Species,
    Municipality,
}

public sealed record NotebookEntry(
    NotebookEntryKind Kind,
    Guid Id,
    string Label,
    string? Sublabel,
    string? Status,
    DateTimeOffset RecordedAtUtc,
    bool HasImage = false
);
