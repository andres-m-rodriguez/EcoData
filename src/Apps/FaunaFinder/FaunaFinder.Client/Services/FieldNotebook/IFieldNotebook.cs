namespace FaunaFinder.Client.Services.FieldNotebook;

/// <summary>
/// The reader's own state, kept in the browser because FaunaFinder has no
/// accounts: what they pinned, and what they have looked at.
///
/// <para>Every method is a no-op that returns empty rather than throwing when
/// storage is unavailable (private browsing, storage disabled). The notebook
/// is secondary chrome and must never take a page down with it.</para>
/// </summary>
public interface IFieldNotebook
{
    /// <summary>Raised after any mutation so open views can refresh.</summary>
    event Action? Changed;

    /// <summary>Pinned entries, most recently pinned first.</summary>
    Task<IReadOnlyList<NotebookEntry>> GetSavedAsync(CancellationToken ct = default);

    /// <summary>Visited entries, most recent first, capped at the newest 20.</summary>
    Task<IReadOnlyList<NotebookEntry>> GetRecentAsync(CancellationToken ct = default);

    /// <summary>Whether this thing is currently pinned.</summary>
    Task<bool> IsSavedAsync(NotebookEntryKind kind, Guid id, CancellationToken ct = default);

    /// <summary>Pins or unpins, and reports the state it landed in.</summary>
    Task<bool> ToggleSavedAsync(NotebookEntry entry, CancellationToken ct = default);

    /// <summary>
    /// Records that the reader opened this thing. Re-visiting moves an entry
    /// to the front rather than duplicating it, and refreshes its labels.
    /// </summary>
    Task RecordVisitAsync(NotebookEntry entry, CancellationToken ct = default);
}
