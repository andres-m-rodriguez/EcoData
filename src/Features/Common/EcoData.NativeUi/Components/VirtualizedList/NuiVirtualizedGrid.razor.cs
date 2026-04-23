using EcoData.Common.Pagination;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;

namespace EcoData.NativeUi.Components.VirtualizedList;

/// <summary>
/// Cursor-paginated virtualized grid. Internally groups items into rows of
/// <see cref="Columns"/> and hands Blazor's <see cref="Virtualize{TItem}"/>
/// a row at a time — this is what keeps Virtualize's scrollbar math correct
/// in a multi-column layout.
///
/// <para>
/// <see cref="ItemSize"/> is the pixel height of a <em>row</em> (card
/// height + vertical gap), not a single card. Consumers should measure and
/// provide this; if it's too small or large, virtualization will jitter or
/// render blank bands during scroll.
/// </para>
/// </summary>
public partial class NuiVirtualizedGrid<TItem, TParams> : ComponentBase
    where TParams : CursorParameters
{
    private readonly List<TItem> _cachedItems = [];
    private Guid? _lastCursor;
    private bool _hasMoreItems = true;
    private bool _isEmpty;
    private bool _isInitialLoading = true;
    private int _generation;
    private Virtualize<IReadOnlyList<TItem>>? _virtualizeRef;

    [Parameter, EditorRequired]
    public required Func<TParams, CancellationToken, IAsyncEnumerable<TItem>> ItemsProvider { get; set; }

    [Parameter, EditorRequired]
    public required Func<Guid?, TParams> ParametersBuilder { get; set; }

    [Parameter, EditorRequired]
    public required Func<TItem, Guid> CursorSelector { get; set; }

    [Parameter, EditorRequired]
    public required RenderFragment<TItem> ItemTemplate { get; set; }

    [Parameter] public RenderFragment? PlaceholderTemplate { get; set; }

    [Parameter] public RenderFragment? LoadingTemplate { get; set; }

    [Parameter] public RenderFragment? EmptyTemplate { get; set; }

    /// <summary>Height of a single row in pixels. Used by Virtualize for scroll math.</summary>
    [Parameter] public float ItemSize { get; set; } = 400;

    /// <summary>Number of extra rows rendered before and after the visible range.</summary>
    [Parameter] public int OverscanCount { get; set; } = 4;

    /// <summary>Columns per row. Defaults to 1 (single-column list).</summary>
    [Parameter] public int? Columns { get; set; }

    /// <summary>Gap between columns within a row.</summary>
    [Parameter] public string Gap { get; set; } = "20px";

    [Parameter] public string? GridClass { get; set; }

    [Parameter] public string? GridStyle { get; set; }

    private int EffectiveColumns => Math.Max(1, Columns ?? 1);

    private string ComputedClass =>
        string.IsNullOrEmpty(GridClass)
            ? "nui-virtualized-grid"
            : $"nui-virtualized-grid {GridClass}";

    private string RowStyle =>
        $"display:grid;grid-template-columns:repeat({EffectiveColumns},1fr);gap:{Gap};";

    public bool IsInitialLoading => _isInitialLoading;

    public bool IsEmpty => _isEmpty;

    protected override async Task OnInitializedAsync()
    {
        await LoadInitialDataAsync();
    }

    private async Task LoadInitialDataAsync()
    {
        var parameters = ParametersBuilder(null);

        await foreach (var item in ItemsProvider(parameters, CancellationToken.None))
        {
            _cachedItems.Add(item);
            _lastCursor = CursorSelector(item);
        }

        if (_cachedItems.Count < parameters.PageSize)
        {
            _hasMoreItems = false;
        }

        _isEmpty = _cachedItems.Count == 0;
        _isInitialLoading = false;
    }

    private async ValueTask<ItemsProviderResult<IReadOnlyList<TItem>>> LoadRowsAsync(
        ItemsProviderRequest request)
    {
        var cols = EffectiveColumns;
        var firstItemIndex = request.StartIndex * cols;
        var lastItemIndex = (request.StartIndex + request.Count) * cols;
        var currentGeneration = _generation;

        while (_hasMoreItems && _cachedItems.Count < lastItemIndex)
        {
            if (_generation != currentGeneration)
            {
                return EmptyResult();
            }

            var parameters = ParametersBuilder(_lastCursor);

            var fetchedCount = 0;
            await foreach (var item in ItemsProvider(parameters, request.CancellationToken))
            {
                if (_generation != currentGeneration)
                {
                    return EmptyResult();
                }

                _cachedItems.Add(item);
                _lastCursor = CursorSelector(item);
                fetchedCount++;
            }

            if (fetchedCount < parameters.PageSize)
            {
                _hasMoreItems = false;
            }
        }

        var rows = new List<IReadOnlyList<TItem>>(request.Count);
        for (var rowIndex = request.StartIndex; rowIndex < request.StartIndex + request.Count; rowIndex++)
        {
            var rowStart = rowIndex * cols;
            if (rowStart >= _cachedItems.Count)
            {
                break;
            }

            var rowEnd = Math.Min(rowStart + cols, _cachedItems.Count);
            rows.Add(_cachedItems.GetRange(rowStart, rowEnd - rowStart));
        }

        var knownRows = (_cachedItems.Count + cols - 1) / cols;
        var totalRowCount = _hasMoreItems ? knownRows + 1 : knownRows;

        return new ItemsProviderResult<IReadOnlyList<TItem>>(rows, totalRowCount);
    }

    private static ItemsProviderResult<IReadOnlyList<TItem>> EmptyResult() =>
        new([], 0);

    /// <summary>
    /// Clears the cache and reloads from the beginning. Call when filter/search params change.
    /// </summary>
    public async Task RefreshAsync()
    {
        _generation++;
        _cachedItems.Clear();
        _lastCursor = null;
        _hasMoreItems = true;
        _isEmpty = false;
        _isInitialLoading = true;
        StateHasChanged();

        await LoadInitialDataAsync();
        StateHasChanged();
    }

    public async void Refresh()
    {
        await RefreshAsync();
    }
}
