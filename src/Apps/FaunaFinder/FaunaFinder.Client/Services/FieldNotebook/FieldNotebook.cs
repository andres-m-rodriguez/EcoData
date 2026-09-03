using System.Text.Json;
using EcoData.Ui.Interop;
using Microsoft.JSInterop;

namespace FaunaFinder.Client.Services.FieldNotebook;

public sealed class FieldNotebook(IJavascriptSafeInterop js) : IFieldNotebook
{
    private const string ModulePath = "./js/fauna-notebook.js";
    private const string SavedKey = "faunafinder-notebook-saved";
    private const string RecentKey = "faunafinder-notebook-recent";

    private const int RecentLimit = 20;

    // JsonSerializerDefaults.Web to match the camelCase, case-insensitive shape
    // the rest of the client speaks — a blob written by an earlier build stays
    // readable.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private IJSObjectReference? _module;

    private bool _moduleUnavailable;

    private List<NotebookEntry>? _saved;
    private List<NotebookEntry>? _recent;

    public event Action? Changed;

    public async Task<IReadOnlyList<NotebookEntry>> GetSavedAsync(CancellationToken ct = default)
        => await LoadSavedAsync(ct);

    public async Task<IReadOnlyList<NotebookEntry>> GetRecentAsync(CancellationToken ct = default)
        => await LoadRecentAsync(ct);

    public async Task<bool> IsSavedAsync(NotebookEntryKind kind, Guid id, CancellationToken ct = default)
    {
        var saved = await LoadSavedAsync(ct);
        return saved.Exists(entry => Matches(entry, kind, id));
    }

    public async Task<bool> ToggleSavedAsync(NotebookEntry entry, CancellationToken ct = default)
    {
        if (entry is null)
            return false;

        var saved = await LoadSavedAsync(ct);
        var wasSaved = saved.Exists(existing => Matches(existing, entry.Kind, entry.Id));

        var next = saved.Where(existing => !Matches(existing, entry.Kind, entry.Id)).ToList();
        if (!wasSaved)
            next.Insert(0, Stamp(entry));

        if (!await WriteAsync(SavedKey, next, ct))
            return wasSaved;

        _saved = next;
        Changed?.Invoke();
        return !wasSaved;
    }

    public async Task RecordVisitAsync(NotebookEntry entry, CancellationToken ct = default)
    {
        if (entry is null)
            return;

        var recent = await LoadRecentAsync(ct);

        var next = new List<NotebookEntry>(Math.Min(recent.Count + 1, RecentLimit)) { Stamp(entry) };
        var kept = recent
            .Where(existing => !Matches(existing, entry.Kind, entry.Id))
            .Take(RecentLimit - 1);
        next.AddRange(kept);

        if (!await WriteAsync(RecentKey, next, ct))
            return;

        _recent = next;
        Changed?.Invoke();
    }

    private static bool Matches(NotebookEntry entry, NotebookEntryKind kind, Guid id)
        => entry.Kind == kind && entry.Id == id;

    private static NotebookEntry Stamp(NotebookEntry entry)
        => entry with { RecordedAtUtc = DateTimeOffset.UtcNow };

    private async Task<List<NotebookEntry>> LoadSavedAsync(CancellationToken ct)
        => _saved ??= await ReadAsync(SavedKey, ct);

    private async Task<List<NotebookEntry>> LoadRecentAsync(CancellationToken ct)
    {
        if (_recent is not null)
            return _recent;

        var stored = await ReadAsync(RecentKey, ct);
        _recent = stored.Count > RecentLimit ? stored.GetRange(0, RecentLimit) : stored;
        return _recent;
    }

    private async Task<List<NotebookEntry>> ReadAsync(string key, CancellationToken ct)
    {
        var module = await GetModuleAsync(ct);
        if (module is null)
            return [];

        // Taken as a raw JsonElement so the interop layer can't fail on a shape
        // it doesn't recognise — the deserialize below is ours to guard.
        var read = await js.InvokeAsync<JsonElement>(module, "read", ct, key);
        if (!read.TryPickT0(out var payload, out _) || payload.ValueKind != JsonValueKind.Array)
            return [];

        try
        {
            var stored = JsonSerializer.Deserialize<List<NotebookEntry?>>(payload, JsonOptions);
            return stored is null ? [] : [.. stored.OfType<NotebookEntry>()];
        }
        catch (JsonException)
        {
            // The schema moved under an existing reader: their stored blob no
            // longer maps onto NotebookEntry. Degrade to an empty notebook; the
            // next write replaces the blob with the current shape.
            return [];
        }
    }

    private async Task<bool> WriteAsync(string key, List<NotebookEntry> entries, CancellationToken ct)
    {
        var module = await GetModuleAsync(ct);
        if (module is null)
            return false;

        var json = JsonSerializer.Serialize(entries, JsonOptions);
        var written = await js.InvokeVoidAsync(module, "write", ct, key, json);

        return written.IsT0;
    }

    private async Task<IJSObjectReference?> GetModuleAsync(CancellationToken ct)
    {
        if (_module is not null)
            return _module;

        if (_moduleUnavailable)
            return null;

        var imported = await js.ImportAsync(ModulePath, ct);
        if (imported.TryPickT0(out var module, out var failure))
        {
            _module = module;
            return module;
        }

        // A missing script is permanent; interop not being callable yet (a
        // component asking too early) or a cancelled call is not, so the next
        // call retries those.
        _moduleUnavailable = failure.Kind == JsFailureKind.ScriptError;

        return null;
    }
}
