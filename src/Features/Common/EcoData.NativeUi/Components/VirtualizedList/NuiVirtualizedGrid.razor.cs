using EcoData.Common.Pagination;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;

namespace EcoData.NativeUi.Components.VirtualizedList;

/// <summary>
/// Cursor-paginated virtualized grid. Wraps Blazor's <see cref="Virtualize{TItem}"/>
/// inside a container styled via <see cref="GridClass"/> / <see cref="GridStyle"/>.
///
/// To compose with a multi-column CSS grid, make the two spacer &lt;div&gt;s that
/// Virtualize renders span the full width, e.g.:
/// <code>
///   .my-grid > div { grid-column: 1 / -1; }
///   .my-grid > .my-card { grid-column: auto; }
/// </code>
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
    private Virtualize<TItem>? _virtualizeRef;

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

    [Parameter] public float ItemSize { get; set; } = 300;

    [Parameter] public int OverscanCount { get; set; } = 6;

    /// <summary>
    /// Number of grid columns. When set, the component emits
    /// <c>grid-template-columns: repeat(N, 1fr)</c> on the host container
    /// and makes the Virtualize spacers span the full row. Leave null to
    /// provide your own grid layout via <see cref="GridClass"/>.
    /// </summary>
    [Parameter] public int? Columns { get; set; }

    /// <summary>
    /// Gap between grid tracks. Only applied when <see cref="Columns"/> is set.
    /// </summary>
    [Parameter] public string Gap { get; set; } = "20px";

    [Parameter] public string? GridClass { get; set; }

    [Parameter] public string? GridStyle { get; set; }

    private string ComputedClass =>
        Columns is not null
            ? $"nui-virtualized-grid {GridClass}".Trim()
            : GridClass ?? string.Empty;

    private string? ComputedStyle
    {
        get
        {
            if (Columns is not int cols) return GridStyle;
            var layout = $"display:grid;grid-template-columns:repeat({cols},1fr);gap:{Gap};";
            return string.IsNullOrEmpty(GridStyle) ? layout : layout + GridStyle;
        }
    }

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
