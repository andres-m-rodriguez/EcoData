using System.Text.Json;
using Microsoft.JSInterop;

namespace FaunaFinder.Client.Services.FieldNotebook;

/// <summary>
/// <see cref="IFieldNotebook"/> backed by browser localStorage, reached through
/// the <c>fauna-notebook.js</c> module.
///
/// <para>Registered as a singleton: this is a WebAssembly client, there is one
/// browser tab behind it, and the library rail has to see writes made by a
/// page. That also lets both lists be cached in memory, so repeated reads in a
/// session never cross the interop boundary — the cache is invalidated by our
/// own writes, which are the only writes there are.</para>
///
/// <para>Storage being unavailable is a normal condition here, not an error:
/// private browsing modes throw on the very first localStorage touch. Every
/// public method absorbs that and reports empty, so a notebook that cannot
/// persist costs the reader a rail, not a page.</para>
/// </summary>
public sealed class FieldNotebook(IJSRuntime js) : IFieldNotebook
{
    private const string ModulePath = "./js/fauna-notebook.js";
    private const string SavedKey = "faunafinder-notebook-saved";
    private const string RecentKey = "faunafinder-notebook-recent";

    /// <summary>How many visits are worth keeping. Older ones fall off the end.</summary>
    private const int RecentLimit = 20;

    // JsonSerializerDefaults.Web to match the camelCase, case-insensitive shape
    // the rest of the client speaks — a blob written by an earlier build stays
    // readable.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private IJSObjectReference? _module;

    // A failed import is permanent for the session: no point paying for a
    // dynamic import on every keystroke once we know it isn't there.
    private bool _moduleUnavailable;

    private List<NotebookEntry>? _saved;
    private List<NotebookEntry>? _recent;

    /// <inheritdoc />
    public event Action? Changed;

    /// <inheritdoc />
    public async Task<IReadOnlyList<NotebookEntry>> GetSavedAsync(CancellationToken ct = default)
        => await LoadSavedAsync(ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<NotebookEntry>> GetRecentAsync(CancellationToken ct = default)
        => await LoadRecentAsync(ct);

    /// <inheritdoc />
    public async Task<bool> IsSavedAsync(NotebookEntryKind kind, Guid id, CancellationToken ct = default)
    {
        var saved = await LoadSavedAsync(ct);
        return saved.Exists(entry => Matches(entry, kind, id));
    }

    /// <inheritdoc />
    public async Task<bool> ToggleSavedAsync(NotebookEntry entry, CancellationToken ct = default)
    {
        if (entry is null)
        {
            return false;
        }

        var saved = await LoadSavedAsync(ct);
        var wasSaved = saved.Exists(existing => Matches(existing, entry.Kind, entry.Id));

        // Either direction drops every row pointing at this thing; pinning then
        // puts one fresh row back at the front, so a re-pin refreshes the labels
        // and reorders rather than stacking up.
        var next = saved.Where(existing => !Matches(existing, entry.Kind, entry.Id)).ToList();
        if (!wasSaved)
        {
            next.Insert(0, Stamp(entry));
        }

        if (!await WriteAsync(SavedKey, next, ct))
        {
            // The write never landed. Report the state actually on disk rather
            // than lighting up a pin that won't survive a reload.
            return wasSaved;
        }

        _saved = next;
        Changed?.Invoke();
        return !wasSaved;
    }

    /// <inheritdoc />
    public async Task RecordVisitAsync(NotebookEntry entry, CancellationToken ct = default)
    {
        if (entry is null)
        {
            return;
        }

        var recent = await LoadRecentAsync(ct);

        // The visit goes to the front and any earlier visit to the same thing
        // is dropped, which both de-duplicates on (Kind, Id) and refreshes the
        // labels from whatever the page just rendered.
        var next = new List<NotebookEntry>(Math.Min(recent.Count + 1, RecentLimit)) { Stamp(entry) };
        next.AddRange(recent
            .Where(existing => !Matches(existing, entry.Kind, entry.Id))
            .Take(RecentLimit - 1));

        if (!await WriteAsync(RecentKey, next, ct))
        {
            return;
        }

        _recent = next;
        Changed?.Invoke();
    }

    private static bool Matches(NotebookEntry entry, NotebookEntryKind kind, Guid id)
        => entry.Kind == kind && entry.Id == id;

    /// <summary>
    /// The notebook owns "when": callers supply the labels, we supply the
    /// moment they were captured, so list order and timestamp can't disagree.
    /// </summary>
    private static NotebookEntry Stamp(NotebookEntry entry)
        => entry with { RecordedAtUtc = DateTimeOffset.UtcNow };

    private async Task<List<NotebookEntry>> LoadSavedAsync(CancellationToken ct)
        => _saved ??= await ReadAsync(SavedKey, ct);

    private async Task<List<NotebookEntry>> LoadRecentAsync(CancellationToken ct)
    {
        if (_recent is not null)
        {
            return _recent;
        }

        // A blob written by an older build, or by hand, can be longer than the
        // cap. Trim on the way in so readers never see more than they should.
        var stored = await ReadAsync(RecentKey, ct);
        _recent = stored.Count > RecentLimit ? stored.GetRange(0, RecentLimit) : stored;
        return _recent;
    }

    private async Task<List<NotebookEntry>> ReadAsync(string key, CancellationToken ct)
    {
        var module = await GetModuleAsync(ct);
        if (module is null)
        {
            return [];
        }

        try
        {
            // Taken as a raw JsonElement so the interop layer can't fail on a
            // shape it doesn't recognise — the deserialize below is ours to
            // guard.
            var payload = await module.InvokeAsync<JsonElement>("read", ct, [key]);
            if (payload.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var stored = JsonSerializer.Deserialize<List<NotebookEntry?>>(payload, JsonOptions);

            // Nulls only turn up in a blob nobody sane wrote, but they'd fault
            // on the first comparison, so they never make it into the cache.
            return stored is null ? [] : [.. stored.OfType<NotebookEntry>()];
        }
        catch (JsonException)
        {
            // The schema moved under an existing reader: their stored blob no
            // longer maps onto NotebookEntry. Degrade to an empty notebook; the
            // next write replaces the blob with the current shape.
            return [];
        }
        catch (JSException)
        {
            return [];
        }
        catch (OperationCanceledException)
        {
            return [];
        }
    }

    /// <summary>Returns whether the list actually reached storage.</summary>
    private async Task<bool> WriteAsync(string key, List<NotebookEntry> entries, CancellationToken ct)
    {
        var module = await GetModuleAsync(ct);
        if (module is null)
        {
            return false;
        }

        try
        {
            var json = JsonSerializer.Serialize(entries, JsonOptions);
            await module.InvokeVoidAsync("write", ct, [key, json]);
            return true;
        }
        catch (JSException)
        {
            return false;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private async Task<IJSObjectReference?> GetModuleAsync(CancellationToken ct)
    {
        if (_module is not null)
        {
            return _module;
        }

        if (_moduleUnavailable)
        {
            return null;
        }

        try
        {
            _module = await js.InvokeAsync<IJSObjectReference>("import", ct, [ModulePath]);
            return _module;
        }
        catch (JSException)
        {
            _moduleUnavailable = true;
            return null;
        }
        catch (InvalidOperationException)
        {
            // Interop isn't callable yet (a component asking too early in its
            // lifecycle). Not a permanent condition — leave the flag alone so
            // the next call retries.
            return null;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }
}
