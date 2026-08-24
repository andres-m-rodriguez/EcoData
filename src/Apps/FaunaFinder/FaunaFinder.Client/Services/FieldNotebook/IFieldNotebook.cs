namespace FaunaFinder.Client.Services.FieldNotebook;

public interface IFieldNotebook
{
    event Action? Changed;

    Task<IReadOnlyList<NotebookEntry>> GetSavedAsync(CancellationToken ct = default);

    Task<IReadOnlyList<NotebookEntry>> GetRecentAsync(CancellationToken ct = default);

    Task<bool> IsSavedAsync(NotebookEntryKind kind, Guid id, CancellationToken ct = default);

    Task<bool> ToggleSavedAsync(NotebookEntry entry, CancellationToken ct = default);

    Task RecordVisitAsync(NotebookEntry entry, CancellationToken ct = default);
}
