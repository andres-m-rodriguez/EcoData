using EcoData.Common.Pagination;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;

namespace EcoData.Common.Pagination.Blazor;

/// <summary>
/// A virtualized list component that encapsulates cursor-based pagination state and provides
/// a consistent UI for loading, empty, and filtered-empty states.
/// </summary>
/// <typeparam name="TItem">The type of items in the list.</typeparam>
/// <typeparam name="TParams">The type of pagination parameters.</typeparam>
public partial class EcoDataVirtualizedList<TItem, TParams> : ComponentBase, IDisposable
    where TParams : CursorParameters
{
    private Virtualize<TItem>? _virtualize;
    private CursorPaginationState<TItem, TParams> _paginationState = null!;
    private CancellationTokenSource? _refreshCts;
    private bool _isLoading = true;
    private bool _disposed;

    /// <summary>
    /// Template for rendering each item in the list.
    /// </summary>
    [Parameter, EditorRequired]
    public RenderFragment<TItem> ItemTemplate { get; set; } = null!;

    /// <summary>
    /// Optional template for rendering skeleton loading items.
    /// If not provided, a default skeleton is used.
    /// </summary>
    [Parameter]
    public RenderFragment? SkeletonTemplate { get; set; }

    /// <summary>
    /// Template to display when the list is empty and no filters are active.
    /// </summary>
    [Parameter]
    public RenderFragment? EmptyTemplate { get; set; }

    /// <summary>
    /// Template to display when the list is empty due to active filters.
    /// The context provides a callback to clear filters.
    /// </summary>
    [Parameter]
    public RenderFragment<Func<Task>>? FilteredEmptyTemplate { get; set; }

    /// <summary>
    /// Function that provides items asynchronously based on parameters.
    /// </summary>
    [Parameter, EditorRequired]
    public Func<TParams, CancellationToken, IAsyncEnumerable<TItem>> ItemsProvider { get; set; } = null!;

    /// <summary>
    /// Factory function that creates pagination parameters.
    /// </summary>
    [Parameter, EditorRequired]
    public Func<TParams> ParametersFactory { get; set; } = null!;

    /// <summary>
    /// Function that extracts the unique key (Guid) from an item.
    /// </summary>
    [Parameter, EditorRequired]
    public Func<TItem, Guid> KeySelector { get; set; } = null!;

    /// <summary>
    /// Optional function that determines if any filters are currently active.
    /// Used to decide whether to show EmptyTemplate or FilteredEmptyTemplate.
    /// </summary>
    [Parameter]
    public Func<TParams, bool>? HasActiveFilters { get; set; }

    /// <summary>
    /// Callback invoked when the user requests to clear filters.
    /// </summary>
    [Parameter]
    public EventCallback OnClearFilters { get; set; }

    /// <summary>
    /// The height in pixels of each item. Used by the virtualizer.
    /// </summary>
    [Parameter]
    public float ItemSize { get; set; } = 65;

    /// <summary>
    /// The number of items to render outside the visible viewport.
    /// </summary>
    [Parameter]
    public int OverscanCount { get; set; } = 5;

    /// <summary>
    /// The number of skeleton items to show during initial loading.
    /// </summary>
    [Parameter]
    public int SkeletonCount { get; set; } = 8;

    /// <summary>
    /// Gets the cached items from the pagination state.
    /// </summary>
    public IReadOnlyList<TItem> CachedItems => _paginationState.CachedItems;

    protected override void OnInitialized()
    {
        _paginationState = new CursorPaginationState<TItem, TParams>(KeySelector);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await RefreshAsync();
        }
    }

    /// <summary>
    /// Refreshes the list by resetting pagination state and reloading data.
    /// </summary>
    public async Task RefreshAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _refreshCts?.Cancel();
        _refreshCts = new CancellationTokenSource();
        var token = _refreshCts.Token;

        try
        {
            _isLoading = true;
            StateHasChanged();

            _paginationState.Reset();

            // Pre-fetch initial items
            var request = new ItemsProviderRequest(0, ParametersFactory().PageSize, token);
            await LoadItemsAsync(request);

            token.ThrowIfCancellationRequested();

            _isLoading = false;
            StateHasChanged();

            if (_virtualize is not null)
            {
                await _virtualize.RefreshDataAsync();
            }
        }
        catch (OperationCanceledException)
        {
            // Refresh was cancelled, ignore
        }
    }

    /// <summary>
    /// Updates an item in the cache by its key.
    /// </summary>
    /// <param name="updatedItem">The updated item.</param>
    public void UpdateItem(TItem updatedItem)
    {
        _paginationState.UpdateItem(updatedItem);
        StateHasChanged();
    }

    /// <summary>
    /// Updates an item in the cache using an updater function.
    /// </summary>
    /// <param name="id">The key of the item to update.</param>
    /// <param name="updater">A function that returns the updated item.</param>
    public void UpdateItem(Guid id, Func<TItem, TItem> updater)
    {
        _paginationState.UpdateItem(id, updater);
        StateHasChanged();
    }

    /// <summary>
    /// Removes an item from the cache by its key.
    /// </summary>
    /// <param name="id">The key of the item to remove.</param>
    /// <returns>True if the item was removed; otherwise, false.</returns>
    public bool RemoveItem(Guid id)
    {
        var result = _paginationState.RemoveItem(id);
        if (result)
        {
            StateHasChanged();
        }
        return result;
    }

    private ValueTask<ItemsProviderResult<TItem>> LoadItemsAsync(ItemsProviderRequest request)
    {
        return _paginationState.ProvideItemsAsync(
            request,
            ParametersFactory,
            ItemsProvider);
    }

    private async Task ClearFiltersAsync()
    {
        await OnClearFilters.InvokeAsync();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _refreshCts?.Cancel();
            _refreshCts?.Dispose();
            _paginationState.Dispose();
            _disposed = true;
        }
    }
}
