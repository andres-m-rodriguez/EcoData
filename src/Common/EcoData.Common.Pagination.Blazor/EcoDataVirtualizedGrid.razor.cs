using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;

namespace EcoData.Common.Pagination.Blazor;

public partial class EcoDataVirtualizedGrid<TItem, TParams> : ComponentBase
    where TParams : CursorParameters
{
    private readonly List<TItem> _cachedItems = [];
    private Guid? _lastCursor;
    private bool _hasMoreItems = true;
    private bool _isEmpty;
    private bool _isInitialLoading = true;
    private int _generation;
    private Virtualize<TItem>? _virtualizeRef;

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
    /// Template shown while items are being loaded (placeholder rows).
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

    /// <summary>
    /// The size of each item in pixels for virtualization (row height).
    /// </summary>
    [Parameter]
    public float ItemSize { get; set; } = 300;

    /// <summary>
    /// Number of extra items to render before and after the visible range.
    /// </summary>
    [Parameter]
    public int OverscanCount { get; set; } = 6;

    /// <summary>
    /// CSS class to apply to the grid container.
    /// </summary>
    [Parameter]
    public string? GridClass { get; set; }

    /// <summary>
    /// Inline style to apply to the grid container.
    /// </summary>
    [Parameter]
    public string? GridStyle { get; set; }

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

    private async ValueTask<ItemsProviderResult<TItem>> LoadItemsAsync(ItemsProviderRequest request)
    {
        var startIndex = request.StartIndex;
        var endIndex = startIndex + request.Count;
        var currentGeneration = _generation;

        while (_hasMoreItems && _cachedItems.Count < endIndex)
        {
            if (_generation != currentGeneration)
                return CreateResult(startIndex, request.Count);

            var parameters = ParametersBuilder(_lastCursor);

            var fetchedCount = 0;
            await foreach (var item in ItemsProvider(parameters, request.CancellationToken))
            {
                if (_generation != currentGeneration)
                    return CreateResult(startIndex, request.Count);

                _cachedItems.Add(item);
                _lastCursor = CursorSelector(item);
                fetchedCount++;
            }

            if (fetchedCount < parameters.PageSize)
            {
                _hasMoreItems = false;
            }
        }

        return CreateResult(startIndex, request.Count);
    }

    private ItemsProviderResult<TItem> CreateResult(int startIndex, int count)
    {
        var items = _cachedItems.Skip(startIndex).Take(count).ToList();
        var totalCount = _hasMoreItems ? _cachedItems.Count + 1 : _cachedItems.Count;
        return new ItemsProviderResult<TItem>(items, totalCount);
    }

    /// <summary>
    /// Refreshes the grid by clearing the cache and reloading from the beginning.
    /// Call this when parameters change (e.g., filters, search text).
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
