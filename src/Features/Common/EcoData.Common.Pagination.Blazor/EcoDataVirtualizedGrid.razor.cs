using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;

namespace EcoData.Common.Pagination.Blazor;

/// <summary>
/// Cursor-paginated virtualized grid. Internally groups items into rows of
/// <see cref="Columns"/> and hands Blazor's <see cref="Virtualize{TItem}"/>
/// a row at a time — this is what keeps Virtualize's scrollbar math correct
/// in a multi-column layout. With the default single column it behaves like
/// <see cref="EcoDataVirtualizedList{TItem, TParams}"/> plus a row wrapper.
///
/// <para>
/// <see cref="ItemSize"/> is the pixel height of a <em>row</em> (card
/// height + vertical gap), not a single card. Consumers should measure and
/// provide this; if it's too small or large, virtualization will jitter or
/// render blank bands during scroll.
/// </para>
///
/// <para>
/// Row layout is class-driven (<c>ecodata-virtualized-grid-row</c> in the
/// scoped stylesheet); the data-driven column count and gap reach CSS through
/// custom properties on the row element.
/// </para>
/// </summary>
public partial class EcoDataVirtualizedGrid<TItem, TParams> : ComponentBase
    where TParams : CursorParameters
{
    private readonly List<TItem> _cachedItems = [];
    private Guid? _lastCursor;
    private bool _hasMoreItems = true;
    private bool _isEmpty;
    private bool _isInitialLoading = true;
    private int _generation;
    private Virtualize<IReadOnlyList<TItem>>? _virtualizeRef;

    /// <summary>
    /// Function that provides items as an async enumerable given the parameters.
    /// </summary>
    [Parameter, EditorRequired]
    public required Func<TParams, CancellationToken, IAsyncEnumerable<TItem>> ItemsProvider { get; set; }

    /// <summary>
    /// Function that builds the parameters for a request, given an optional cursor.
    /// </summary>
    [Parameter, EditorRequired]
    public required Func<Guid?, TParams> ParametersBuilder { get; set; }

    /// <summary>
    /// Function that extracts the cursor value from an item (typically the Id).
    /// </summary>
    [Parameter, EditorRequired]
    public required Func<TItem, Guid> CursorSelector { get; set; }

    /// <summary>
    /// Template for rendering each item.
    /// </summary>
    [Parameter, EditorRequired]
    public required RenderFragment<TItem> ItemTemplate { get; set; }

    /// <summary>
    /// Template shown for each not-yet-loaded cell while a row is being fetched.
    /// </summary>
    [Parameter]
    public RenderFragment? PlaceholderTemplate { get; set; }

    /// <summary>
    /// Template shown during initial loading (skeleton state).
    /// </summary>
    [Parameter]
    public RenderFragment? LoadingTemplate { get; set; }

    /// <summary>
    /// Template shown when there are no items.
    /// </summary>
    [Parameter]
    public RenderFragment? EmptyTemplate { get; set; }

    /// <summary>Height of a single row in pixels. Used by Virtualize for scroll math.</summary>
    [Parameter]
    public float ItemSize { get; set; } = 400;

    /// <summary>Number of extra rows rendered before and after the visible range.</summary>
    [Parameter]
    public int OverscanCount { get; set; } = 4;

    /// <summary>Columns per row. Defaults to 1 (single-column list).</summary>
    [Parameter]
    public int? Columns { get; set; }

    /// <summary>Gap between columns within a row (any CSS length).</summary>
    [Parameter]
    public string Gap { get; set; } = "20px";

    /// <summary>
    /// Extra CSS class for the grid container.
    /// </summary>
    [Parameter]
    public string? GridClass { get; set; }

    private int EffectiveColumns => Math.Max(1, Columns ?? 1);

    private string ComputedClass =>
        string.IsNullOrEmpty(GridClass)
            ? "ecodata-virtualized-grid"
            : $"ecodata-virtualized-grid {GridClass}";

    // Data-driven values only — the layout rules live in the scoped stylesheet.
    private string RowVariables =>
        $"--grid-cols: {EffectiveColumns}; --grid-gap: {Gap};";

    /// <summary>
    /// Whether the grid is currently in initial loading state.
    /// </summary>
    public bool IsInitialLoading => _isInitialLoading;

    /// <summary>
    /// Whether the grid is empty (no items after loading).
    /// </summary>
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

    /// <summary>
    /// Synchronous refresh that can be used from event handlers.
    /// </summary>
    public async void Refresh()
    {
        await RefreshAsync();
    }
}
